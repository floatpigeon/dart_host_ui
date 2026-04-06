namespace DartGui.App.ViewModels;

public sealed class CameraCardViewModel : ViewModelBase
{
    private bool isOnline_;
    private string resolution_ = "--";
    private string placeholderText_ = "预留画面区域";

    public CameraCardViewModel(string displayName, bool isOnline, string resolution, string placeholderText)
    {
        DisplayName = displayName;
        SetPreviewState(isOnline, resolution, placeholderText);
    }

    public string DisplayName { get; }

    public bool IsOnline
    {
        get => isOnline_;
        private set
        {
            if (SetProperty(ref isOnline_, value))
            {
                OnPropertyChanged(nameof(IsOffline));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public bool IsOffline => !IsOnline;

    public string Resolution
    {
        get => resolution_;
        private set => SetProperty(ref resolution_, value);
    }

    public string PlaceholderText
    {
        get => placeholderText_;
        private set => SetProperty(ref placeholderText_, value);
    }

    public string StatusText => IsOnline ? "在线" : "离线";

    public void SetPreviewState(bool isOnline, string resolution, string placeholderText)
    {
        IsOnline = isOnline;
        Resolution = resolution;
        PlaceholderText = placeholderText;
    }
}
