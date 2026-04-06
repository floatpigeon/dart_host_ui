namespace DartGui.App.ViewModels;

public sealed record TaskQueueItemViewModel(
    string Title,
    string StatusText,
    string DetailText,
    bool IsRunning,
    bool IsQueued,
    bool IsIdle);
