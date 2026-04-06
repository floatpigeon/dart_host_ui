using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DartGui.App.Models;

namespace DartGui.App.Services;

public sealed class GuiBridgeClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Uri endpoint_;
    private readonly CancellationTokenSource shutdownCts_ = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BridgeCommandAck>> pendingAcks_ =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim sendLock_ = new(1, 1);

    private readonly Task runTask_;
    private ClientWebSocket? socket_;
    private int disposed_;

    public GuiBridgeClient(Uri endpoint)
    {
        endpoint_ = endpoint;
        runTask_ = Task.Run(() => RunAsync(shutdownCts_.Token));
    }

    public event Action<ConnectionState>? ConnectionStateChanged;

    public event Action<BridgeStateSnapshot>? SnapshotReceived;

    public event Action<BridgeFaultInfo>? FaultEventReceived;

    public event Action<long>? FaultClearedReceived;

    public async Task<BridgeCommandAck> SendTaskCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        var socket = socket_;
        if (socket is null || socket.State != WebSocketState.Open)
        {
            return new BridgeCommandAck(false, "bridge_offline");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<BridgeCommandAck>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        pendingAcks_[requestId] = completion;

        try
        {
            await SendJsonAsync(
                socket,
                $$"""
                {"type":"task.submit","request_id":"{{requestId}}","command":"{{command}}"}
                """,
                cancellationToken);

            return await completion.Task.WaitAsync(cancellationToken);
        }
        catch
        {
            pendingAcks_.TryRemove(requestId, out _);
            return new BridgeCommandAck(false, "send_failed");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed_, 1) != 0)
        {
            return;
        }

        shutdownCts_.Cancel();
        FailPendingAcks("bridge_shutdown");

        try
        {
            socket_?.Abort();
        }
        catch
        {
        }

        try
        {
            runTask_.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        socket_?.Dispose();
        sendLock_.Dispose();
        shutdownCts_.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ClientWebSocket? socket = null;
            try
            {
                SetConnectionState(ConnectionState.Reconnecting);

                socket = new ClientWebSocket();
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                await socket.ConnectAsync(endpoint_, cancellationToken);

                socket_ = socket;
                SetConnectionState(ConnectionState.Online);

                await ReceiveLoopAsync(socket, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            finally
            {
                socket_ = null;
                FailPendingAcks("bridge_disconnected");
                SetConnectionState(ConnectionState.Offline);

                if (socket is not null)
                {
                    socket.Dispose();
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using var payloadBuffer = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                payloadBuffer.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    break;
                }
            }

            var payload = Encoding.UTF8.GetString(payloadBuffer.ToArray());
            HandleMessage(payload);
        }
    }

    private void HandleMessage(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("type", out var typeProperty))
            {
                return;
            }

            var type = typeProperty.GetString();
            switch (type)
            {
                case "state.snapshot":
                {
                    var snapshot = JsonSerializer.Deserialize<BridgeStateSnapshot>(payload, JsonOptions);
                    if (snapshot is not null)
                    {
                        SnapshotReceived?.Invoke(snapshot);
                    }
                    break;
                }
                case "fault.event":
                {
                    var fault = JsonSerializer.Deserialize<BridgeFaultInfo>(payload, JsonOptions);
                    if (fault is not null)
                    {
                        FaultEventReceived?.Invoke(fault);
                    }
                    break;
                }
                case "fault.cleared":
                {
                    if (document.RootElement.TryGetProperty("timestamp_ms", out var timestampProperty)
                        && timestampProperty.TryGetInt64(out var timestampMs))
                    {
                        FaultClearedReceived?.Invoke(timestampMs);
                    }
                    break;
                }
                case "command.ack":
                {
                    var ack = JsonSerializer.Deserialize<BridgeCommandAckMessage>(payload, JsonOptions);
                    if (ack is not null
                        && pendingAcks_.TryRemove(ack.RequestId, out var completion))
                    {
                        completion.TrySetResult(new BridgeCommandAck(ack.Accepted, ack.Reason));
                    }
                    break;
                }
            }
        }
        catch (JsonException)
        {
        }
    }

    private async Task SendJsonAsync(
        ClientWebSocket socket,
        string payload,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await sendLock_.WaitAsync(cancellationToken);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }
        finally
        {
            sendLock_.Release();
        }
    }

    private void SetConnectionState(ConnectionState state)
    {
        ConnectionStateChanged?.Invoke(state);
    }

    private void FailPendingAcks(string reason)
    {
        foreach (var (requestId, completion) in pendingAcks_)
        {
            if (pendingAcks_.TryRemove(requestId, out _))
            {
                completion.TrySetResult(new BridgeCommandAck(false, reason));
            }
        }
    }
}
