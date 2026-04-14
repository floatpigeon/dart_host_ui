using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DartHost.App.Transport;

public sealed class DartTcpClient : IDartHostClient
{
    private readonly DartClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _readLoopCancellation;
    private Task? _readLoopTask;
    private TaskCompletionSource<ManagerStatePayload>? _initialManagerState;
    private long _requestCounter;

    public DartTcpClient(DartClientOptions options)
    {
        _options = options;
    }

    public event Action<DartConnectionState>? ConnectionStateChanged;
    public event Action<ManagerStatePayload>? ManagerStateReceived;
    public event Action<ErrorPayload>? ErrorReceived;
    public event Action<HeartbeatPayload>? HeartbeatReceived;

    public async Task<HelloAckPayload> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_tcpClient is not null)
        {
            throw new InvalidOperationException("Client is already connected.");
        }

        ConnectionStateChanged?.Invoke(DartConnectionState.Connecting);

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_options.Host, _options.Port, cancellationToken);

        _networkStream = _tcpClient.GetStream();
        _reader = new StreamReader(_networkStream, Encoding.UTF8, leaveOpen: true);
        _writer = new StreamWriter(_networkStream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true
        };

        _readLoopCancellation = new CancellationTokenSource();
        _initialManagerState =
            new TaskCompletionSource<ManagerStatePayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        _readLoopTask = Task.Run(() => ReadLoopAsync(_readLoopCancellation.Token), CancellationToken.None);

        var requestId = NextRequestId("hello");
        var helloEnvelope = new
        {
            type = "hello",
            protocol_version = 1,
            request_id = requestId,
            timestamp_ms = TimestampMs(),
            payload = new
            {
                client_name = _options.ClientName,
                client_version = _options.ClientVersion,
                capabilities = _options.EffectiveCapabilities
            }
        };

        var helloResponse = await SendRequestAsync(requestId, helloEnvelope, cancellationToken);
        var helloAck = DeserializePayload<HelloAckPayload>(helloResponse);
        await _initialManagerState.Task.WaitAsync(cancellationToken);
        ConnectionStateChanged?.Invoke(DartConnectionState.Connected);
        return helloAck;
    }

    public async Task<CommandAckPayload> SendCommandAsync(
        string commandName,
        CancellationToken cancellationToken = default)
    {
        var requestId = NextRequestId("cmd");
        var commandEnvelope = new
        {
            type = "command",
            protocol_version = 1,
            request_id = requestId,
            timestamp_ms = TimestampMs(),
            payload = new
            {
                name = commandName,
                args = new { }
            }
        };

        var response = await SendRequestAsync(requestId, commandEnvelope, cancellationToken);
        return DeserializePayload<CommandAckPayload>(response);
    }

    public async ValueTask DisposeAsync()
    {
        if (_readLoopCancellation is not null)
        {
            _readLoopCancellation.Cancel();
        }

        if (_readLoopTask is not null)
        {
            try
            {
                await _readLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var pending in _pendingRequests.Values)
        {
            pending.TrySetCanceled();
        }

        _pendingRequests.Clear();
        _initialManagerState?.TrySetCanceled();
        _sendLock.Dispose();

        if (_writer is not null)
        {
            await _writer.DisposeAsync();
        }

        _reader?.Dispose();
        if (_networkStream is not null)
        {
            await _networkStream.DisposeAsync();
        }

        _tcpClient?.Dispose();
        _readLoopCancellation?.Dispose();
        ConnectionStateChanged?.Invoke(DartConnectionState.Disconnected);
    }

    private async Task<string> SendRequestAsync(
        string requestId,
        object envelope,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var completion =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(requestId, completion))
        {
            throw new InvalidOperationException($"Duplicate request id '{requestId}'.");
        }

        try
        {
            var serialized = JsonSerializer.Serialize(envelope, _jsonOptions);
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                await _writer!.WriteLineAsync(serialized.AsMemory(), cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }

            var responseLine = await completion.Task.WaitAsync(cancellationToken);
            using var responseDocument = JsonDocument.Parse(responseLine);
            var responseType = responseDocument.RootElement.GetProperty("type").GetString() ?? "";
            if (responseType == "error")
            {
                var error = DeserializePayload<ErrorPayload>(responseLine);
                throw new DartProtocolException(error.Code, error.Message);
            }

            return responseLine;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _reader!.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    throw new IOException("Remote host closed the connection.");
                }

                HandleInboundLine(line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            foreach (var pending in _pendingRequests.Values)
            {
                pending.TrySetException(new IOException("Read loop stopped unexpectedly."));
            }

            _initialManagerState?.TrySetException(new IOException("Initial manager state was not received."));
            ConnectionStateChanged?.Invoke(DartConnectionState.Faulted);
        }
    }

    private void HandleInboundLine(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString() ?? "";
        var requestId = root.GetProperty("request_id").GetString() ?? "";

        if (!string.IsNullOrWhiteSpace(requestId)
            && _pendingRequests.TryGetValue(requestId, out var pendingRequest))
        {
            pendingRequest.TrySetResult(line);
        }

        switch (type)
        {
            case "manager_state":
            {
                var payload = DeserializePayload<ManagerStatePayload>(line);
                ManagerStateReceived?.Invoke(payload);
                _initialManagerState?.TrySetResult(payload);
                break;
            }
            case "heartbeat":
            {
                var payload = DeserializePayload<HeartbeatPayload>(line);
                HeartbeatReceived?.Invoke(payload);
                break;
            }
            case "error":
            {
                var payload = DeserializePayload<ErrorPayload>(line);
                ErrorReceived?.Invoke(payload);
                break;
            }
        }
    }

    private static T DeserializePayload<T>(string line)
    {
        using var document = JsonDocument.Parse(line);
        return DeserializePayload<T>(document.RootElement);
    }

    private static T DeserializePayload<T>(JsonElement envelope)
    {
        if (!envelope.TryGetProperty("payload", out var payload))
        {
            throw new InvalidOperationException("Envelope payload is missing.");
        }

        var result = payload.Deserialize<T>();
        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload as {typeof(T).Name}.");
        }

        return result;
    }

    private void EnsureConnected()
    {
        if (_tcpClient is null || _writer is null || _reader is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }
    }

    private string NextRequestId(string prefix)
    {
        var value = Interlocked.Increment(ref _requestCounter);
        return $"{prefix}-{value}";
    }

    private static long TimestampMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
