using CommunityToolkit.Mvvm.Input;

namespace DartGui.App.ViewModels;

public sealed class TaskCommandButtonViewModel : ViewModelBase
{
    private readonly string defaultStatusText_;
    private bool isActive_;
    private string statusText_;

    public TaskCommandButtonViewModel(
        string commandName,
        string displayName,
        Action<TaskCommandButtonViewModel> sendHandler,
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
        SendCommandCommand = new RelayCommand(() => sendHandler(this));
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

    public bool IsEnabled => true;

    public bool IsPending => false;

    public bool IsFailed => false;

    public bool IsUnsupported => false;

    public string StatusText
    {
        get => statusText_;
        private set => SetProperty(ref statusText_, value);
    }

    public IRelayCommand SendCommandCommand { get; }

    public void SetSelected(bool selected)
    {
        IsActive = selected;
        StatusText = selected ? "已选择" : defaultStatusText_;
    }
}
