namespace DartGui.App.ViewModels;

public sealed class ManualButtonViewModel : ViewModelBase
{
    private bool isActive_;

    public ManualButtonViewModel(string groupKey, string direction, string displayName)
    {
        GroupKey = groupKey;
        Direction = direction;
        DisplayName = displayName;
    }

    public string GroupKey { get; }

    public string Direction { get; }

    public string DisplayName { get; }

    public bool IsActive
    {
        get => isActive_;
        set
        {
            if (SetProperty(ref isActive_, value))
            {
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public bool IsEnabled => true;
}
