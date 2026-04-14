namespace DartHost.App.State;

public sealed record QueueItemState(
    string TaskName,
    string DisplayName,
    string Status);

public sealed record LastErrorState(
    string TaskName,
    string ActionName,
    string Reason,
    string Message,
    long TimestampMs);

public sealed record ManagerStatusState(
    string LifecycleState,
    string CurrentTask,
    string CurrentAction,
    uint FireCount,
    IReadOnlyList<QueueItemState> Queue,
    LastErrorState? LastError)
{
    public static ManagerStatusState Empty =>
        new(
            LifecycleState: "UNKNOWN",
            CurrentTask: "",
            CurrentAction: "",
            FireCount: 0,
            Queue: [],
            LastError: null);
}
