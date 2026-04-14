using DartHost.App.State;
using DartHost.App.Transport;

namespace DartHost.App.Services;

public sealed class HostConnectionService : IHostConnectionService
{
    private readonly HostConnectionOptions _options;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly IReadOnlySet<string> _emptyCommandSet = new HashSet<string>(StringComparer.Ordinal);

    private IDartHostClient? _client;
    private Task? _reconnectTask;
    private bool _manualDisconnectRequested;
    private bool _disposed;

    public HostConnectionService(HostConnectionOptions? options = null)
    {
        _options = options ?? new HostConnectionOptions();
        ConnectionStatus = HostConnectionStatus.Disconnected;
        ConnectionDetail = $"Waiting for the bridge at {_options.ClientOptions.Host}:{_options.ClientOptions.Port}";
        GlobalErrorMessage = "";
        SessionId = "";
        SupportedCommands = _emptyCommandSet;
        ManagerStatus = ManagerStatusState.Empty;
        FeatureAvailability = FeatureAvailabilityState.Default;
    }

    public event EventHandler? StateChanged;

    public HostConnectionStatus ConnectionStatus { get; private set; }

    public string ConnectionDetail { get; private set; }

    public string GlobalErrorMessage { get; private set; }

    public string SessionId { get; private set; }

    public DateTimeOffset? LastHeartbeatUtc { get; private set; }

    public HelloAckPayload? HelloAck { get; private set; }

    public IReadOnlySet<string> SupportedCommands { get; private set; }

    public ManagerStatusState ManagerStatus { get; private set; }

    public FeatureAvailabilityState FeatureAvailability { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _manualDisconnectRequested = false;
        ScheduleReconnect(TimeSpan.Zero);
        return Task.CompletedTask;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _manualDisconnectRequested = false;
        await ConnectCoreAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _manualDisconnectRequested = true;

        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            await ResetClientAsync();
            ConnectionStatus = HostConnectionStatus.Disconnected;
            ConnectionDetail = "Disconnected from the RMCS bridge.";
            GlobalErrorMessage = "";
            LastHeartbeatUtc = null;
            FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
            RaiseStateChanged();
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async Task<CommandAckPayload> SendCommandAsync(
        string commandName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        IDartHostClient client;
        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_client is null || ConnectionStatus != HostConnectionStatus.Connected)
            {
                throw new InvalidOperationException("The host is not connected to RMCS.");
            }

            client = _client;
        }
        finally
        {
            _connectionGate.Release();
        }

        try
        {
            var response = await client.SendCommandAsync(commandName, cancellationToken);
            GlobalErrorMessage = response.Accepted
                ? ""
                : $"Command '{commandName}' was rejected by the bridge.";
            RaiseStateChanged();
            return response;
        }
        catch (Exception exception)
        {
            GlobalErrorMessage = $"Command '{commandName}' failed: {exception.Message}";
            RaiseStateChanged();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _manualDisconnectRequested = true;
        _lifetimeCancellation.Cancel();

        if (_reconnectTask is not null)
        {
            try
            {
                await _reconnectTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        await _connectionGate.WaitAsync();
        try
        {
            await ResetClientAsync();
        }
        finally
        {
            _connectionGate.Release();
        }

        _connectionGate.Dispose();
        _lifetimeCancellation.Dispose();
    }

    private void ScheduleReconnect(TimeSpan delay)
    {
        if (_disposed || _manualDisconnectRequested)
        {
            return;
        }

        if (_reconnectTask is { IsCompleted: false })
        {
            return;
        }

        _reconnectTask = Task.Run(async () =>
        {
            try
            {
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, _lifetimeCancellation.Token);
                }

                while (!_lifetimeCancellation.IsCancellationRequested
                    && !_manualDisconnectRequested
                    && ConnectionStatus != HostConnectionStatus.Connected)
                {
                    try
                    {
                        await ConnectCoreAsync(_lifetimeCancellation.Token);
                        return;
                    }
                    catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        ConnectionStatus = HostConnectionStatus.Faulted;
                        ConnectionDetail =
                            $"Retrying {_options.ClientOptions.Host}:{_options.ClientOptions.Port} in {_options.ReconnectDelay.TotalSeconds:0.#}s";
                        GlobalErrorMessage = $"Connection failed: {exception.Message}";
                        FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
                        RaiseStateChanged();
                        await ResetClientSafeAsync();
                        await Task.Delay(_options.ReconnectDelay, _lifetimeCancellation.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, _lifetimeCancellation.Token);
    }

    private async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (ConnectionStatus == HostConnectionStatus.Connected && _client is not null)
            {
                return;
            }

            await ResetClientAsync();

            ConnectionStatus = HostConnectionStatus.Connecting;
            ConnectionDetail = $"Connecting to {_options.ClientOptions.Host}:{_options.ClientOptions.Port}";
            GlobalErrorMessage = "";
            FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
            RaiseStateChanged();

            var client = new DartTcpClient(_options.ClientOptions);
            Subscribe(client);
            _client = client;

            var helloAck = await client.ConnectAsync(cancellationToken);
            HelloAck = helloAck;
            SessionId = helloAck.SessionId;
            SupportedCommands = new HashSet<string>(helloAck.SupportedCommands, StringComparer.Ordinal);
            ConnectionStatus = HostConnectionStatus.Connected;
            ConnectionDetail = $"{helloAck.ServerName} {helloAck.ServerVersion} on session {helloAck.SessionId}";
            GlobalErrorMessage = "";
            FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
            RaiseStateChanged();
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private void Subscribe(IDartHostClient client)
    {
        client.ConnectionStateChanged += OnTransportConnectionStateChanged;
        client.ManagerStateReceived += OnManagerStateReceived;
        client.ErrorReceived += OnErrorReceived;
        client.HeartbeatReceived += OnHeartbeatReceived;
    }

    private void Unsubscribe(IDartHostClient client)
    {
        client.ConnectionStateChanged -= OnTransportConnectionStateChanged;
        client.ManagerStateReceived -= OnManagerStateReceived;
        client.ErrorReceived -= OnErrorReceived;
        client.HeartbeatReceived -= OnHeartbeatReceived;
    }

    private void OnTransportConnectionStateChanged(DartConnectionState state)
    {
        switch (state)
        {
            case DartConnectionState.Connecting:
                ConnectionStatus = HostConnectionStatus.Connecting;
                ConnectionDetail = $"Connecting to {_options.ClientOptions.Host}:{_options.ClientOptions.Port}";
                FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
                RaiseStateChanged();
                break;
            case DartConnectionState.Connected:
                ConnectionStatus = HostConnectionStatus.Connected;
                FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
                RaiseStateChanged();
                break;
            case DartConnectionState.Faulted:
                _ = HandleTransportDropAsync(
                    HostConnectionStatus.Faulted,
                    "The bridge connection dropped unexpectedly.");
                break;
            case DartConnectionState.Disconnected:
                if (!_manualDisconnectRequested)
                {
                    _ = HandleTransportDropAsync(
                        HostConnectionStatus.Disconnected,
                        "The bridge disconnected.");
                }

                break;
        }
    }

    private void OnManagerStateReceived(ManagerStatePayload payload)
    {
        ManagerStatus = new ManagerStatusState(
            payload.LifecycleState,
            payload.CurrentTask,
            payload.CurrentAction,
            payload.FireCount,
            payload.Queue.Select(item => new QueueItemState(item.TaskName, item.DisplayName, item.Status)).ToArray(),
            payload.LastError is null
                ? null
                : new LastErrorState(
                    payload.LastError.TaskName,
                    payload.LastError.ActionName,
                    payload.LastError.Reason,
                    payload.LastError.Message,
                    payload.LastError.TimestampMs));

        RaiseStateChanged();
    }

    private void OnErrorReceived(ErrorPayload payload)
    {
        GlobalErrorMessage = $"{payload.Code}: {payload.Message}";
        RaiseStateChanged();
    }

    private void OnHeartbeatReceived(HeartbeatPayload payload)
    {
        SessionId = payload.SessionId;
        LastHeartbeatUtc = DateTimeOffset.Now;
        RaiseStateChanged();
    }

    private async Task HandleTransportDropAsync(
        HostConnectionStatus status,
        string detail)
    {
        if (_disposed)
        {
            return;
        }

        await _connectionGate.WaitAsync();
        try
        {
            await ResetClientAsync();
            ConnectionStatus = status;
            ConnectionDetail = detail;
            if (!_manualDisconnectRequested)
            {
                GlobalErrorMessage = $"{detail} Reconnecting automatically.";
            }

            FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);
            RaiseStateChanged();
        }
        finally
        {
            _connectionGate.Release();
        }

        if (!_manualDisconnectRequested)
        {
            ScheduleReconnect(_options.ReconnectDelay);
        }
    }

    private async Task ResetClientSafeAsync()
    {
        await _connectionGate.WaitAsync();
        try
        {
            await ResetClientAsync();
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private async Task ResetClientAsync()
    {
        var client = _client;
        _client = null;

        HelloAck = null;
        SupportedCommands = _emptyCommandSet;
        SessionId = "";
        LastHeartbeatUtc = null;
        FeatureAvailability = BuildFeatureAvailability(ConnectionStatus);

        if (client is null)
        {
            return;
        }

        Unsubscribe(client);
        await client.DisposeAsync();
    }

    private FeatureAvailabilityState BuildFeatureAvailability(HostConnectionStatus status)
    {
        var taskControlAvailable = status == HostConnectionStatus.Connected;
        return new FeatureAvailabilityState(
            TaskControlAvailable: taskControlAvailable,
            StepControlAvailable: false,
            ManualControlAvailable: false,
            DeviceRawStateAvailable: false,
            VisualFeedbackAvailable: false);
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
