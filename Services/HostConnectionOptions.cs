using DartHost.App.Transport;

namespace DartHost.App.Services;

public sealed class HostConnectionOptions
{
    public DartClientOptions ClientOptions { get; init; } = new();

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);
}
