using CommunityToolkit.Mvvm.Input;

namespace DartGui.App.ViewModels;

public sealed class TaskCommandButtonViewModel : ViewModelBase
{
    private readonly string defaultStatusText_;
    private bool isActive_;
    private bool isEnabled_ = true;
    private bool isFailed_;
    private bool isPending_;
    private string statusText_;

    public TaskCommandButtonViewModel(
        string commandName,
        string displayName,
        Func<TaskCommandButtonViewModel, Task> sendHandler,
        bool isDanger = false,
        bool isRecovery = false,
        string defaultStatusText = "预览")
    {
        CommandName = commandName;
        DisplayName = displayName;
        IsDanger = isDanger;
        IsRecovery = isRecovery;
        defaultStatusText_ = defaultStatusText;
        statusText_ = defaultStatusText;
        SendCommandCommand = new AsyncRelayCommand(() => sendHandler(this));
    }

    public string CommandName { get; }

    public string DisplayName { get; }

    public bool IsDanger { get; }

    public bool IsRecovery { get; }

    public bool IsActive
    {
        get => isActive_;
        private set
        {
            SetProperty(ref isActive_, value);
        }
    }

    public bool IsEnabled
    {
        get => isEnabled_;
        private set => SetProperty(ref isEnabled_, value);
    }

    public bool IsPending
    {
        get => isPending_;
        private set => SetProperty(ref isPending_, value);
    }

    public bool IsFailed
    {
        get => isFailed_;
        private set => SetProperty(ref isFailed_, value);
    }

    public bool IsUnsupported => false;

    public string StatusText
    {
        get => statusText_;
        private set => SetProperty(ref statusText_, value);
    }

    public IAsyncRelayCommand SendCommandCommand { get; }

    public void SetSelected(bool selected, string activeStatusText = "已选择")
    {
        IsActive = selected;
        if (!IsPending && !IsFailed)
        {
            StatusText = selected ? activeStatusText : defaultStatusText_;
        }
    }

    public void SetPending()
    {
        IsPending = true;
        IsFailed = false;
        IsActive = true;
        StatusText = "发送中";
    }

    public void SetCommandResult(bool accepted, string acceptedText = "已受理")
    {
        IsPending = false;
        IsFailed = !accepted;
        IsActive = accepted;
        StatusText = accepted ? acceptedText : "发送失败";
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;

        if (enabled)
        {
            if (!IsPending && !IsActive && !IsFailed)
            {
                StatusText = defaultStatusText_;
            }

            return;
        }

        if (!IsPending)
        {
            StatusText = "离线";
        }
    }

    public void ResetState()
    {
        IsPending = false;
        IsFailed = false;
        IsActive = false;
        StatusText = IsEnabled ? defaultStatusText_ : "离线";
    }
}
