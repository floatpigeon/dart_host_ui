using DartHost.App.State;
using DartHost.App.Transport;

namespace DartHost.App.Services;

public interface IHostConnectionService : IAsyncDisposable
{
    event EventHandler? StateChanged;

    HostConnectionStatus ConnectionStatus { get; }
    string ConnectionDetail { get; }
    string GlobalErrorMessage { get; }
    string SessionId { get; }
    DateTimeOffset? LastHeartbeatUtc { get; }
    HelloAckPayload? HelloAck { get; }
    IReadOnlySet<string> SupportedCommands { get; }
    ManagerStatusState ManagerStatus { get; }
    FeatureAvailabilityState FeatureAvailability { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<CommandAckPayload> SendCommandAsync(string commandName, CancellationToken cancellationToken = default);
}
