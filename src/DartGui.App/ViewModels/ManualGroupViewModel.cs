using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class ManualGroupViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<string, ManualButtonViewModel> buttonsByDirection_;

    public ManualGroupViewModel(
        string groupKey,
        string displayName,
        bool usesDpad,
        params (string Direction, string Label)[] buttons)
    {
        GroupKey = groupKey;
        DisplayName = displayName;
        UsesDpad = usesDpad;
        Buttons = new ObservableCollection<ManualButtonViewModel>(
            buttons.Select(button => new ManualButtonViewModel(groupKey, button.Direction, button.Label)));
        buttonsByDirection_ = Buttons.ToDictionary(button => button.Direction, StringComparer.Ordinal);
    }

    public string GroupKey { get; }

    public string DisplayName { get; }

    public bool UsesDpad { get; }

    public bool UsesVerticalLayout => !UsesDpad;

    public ObservableCollection<ManualButtonViewModel> Buttons { get; }

    public string StatusText => UsesDpad ? "十字键位" : "纵向键位";

    public ManualButtonViewModel? UpButton => GetButton("up");

    public ManualButtonViewModel? DownButton => GetButton("down");

    public ManualButtonViewModel? LeftButton => GetButton("left");

    public ManualButtonViewModel? RightButton => GetButton("right");

    public void ResetActiveButtons()
    {
        foreach (var button in Buttons)
        {
            button.IsActive = false;
        }
    }

    private ManualButtonViewModel? GetButton(string direction)
    {
        return buttonsByDirection_.TryGetValue(direction, out var button) ? button : null;
    }
}
