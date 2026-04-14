namespace DartHost.App.State;

public enum HostConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Faulted
}

public enum LeftPanelSelection
{
    TaskControl,
    StepControl,
    ManualControl
}

public enum RightPanelSelection
{
    DeviceStatus,
    VisualFeedback
}

public sealed record AppShellState(
    HostConnectionStatus ConnectionStatus,
    LeftPanelSelection LeftPanel,
    RightPanelSelection RightPanel,
    string GlobalErrorMessage,
    string ConnectionDetail,
    string SessionId,
    DateTimeOffset? LastHeartbeatUtc)
{
    public static AppShellState Default =>
        new(
            HostConnectionStatus.Disconnected,
            LeftPanelSelection.TaskControl,
            RightPanelSelection.DeviceStatus,
            "",
            "Waiting for the bridge at 127.0.0.1:37601",
            "",
            null);
}
