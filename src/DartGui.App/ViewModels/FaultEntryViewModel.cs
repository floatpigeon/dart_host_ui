namespace DartGui.App.ViewModels;

public sealed class FaultEntryViewModel : ViewModelBase
{
    public FaultEntryViewModel(
        string timestampText,
        string source,
        string code,
        string message,
        string statusText,
        string levelText,
        bool isError = false,
        bool isWarning = false,
        bool isRecovered = false)
    {
        TimestampText = timestampText;
        Source = source;
        Code = code;
        Message = message;
        StatusText = statusText;
        LevelText = levelText;
        IsError = isError;
        IsWarning = isWarning;
        IsRecovered = isRecovered;
    }

    public string TimestampText { get; }

    public string Source { get; }

    public string Code { get; }

    public string Message { get; }

    public string StatusText { get; }

    public string LevelText { get; }

    public bool IsError { get; }

    public bool IsWarning { get; }

    public bool IsRecovered { get; }
}
