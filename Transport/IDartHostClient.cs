namespace DartHost.App.Transport;

public interface IDartHostClient : IAsyncDisposable
{
    event Action<DartConnectionState>? ConnectionStateChanged;
    event Action<ManagerStatePayload>? ManagerStateReceived;
    event Action<ErrorPayload>? ErrorReceived;
    event Action<HeartbeatPayload>? HeartbeatReceived;

    Task<HelloAckPayload> ConnectAsync(CancellationToken cancellationToken = default);
    Task<CommandAckPayload> SendCommandAsync(string commandName, CancellationToken cancellationToken = default);
}
