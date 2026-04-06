using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class ManualPageViewModel : ViewModelBase
{
    private readonly Dictionary<string, ManualButtonViewModel> activeButtonsByGroup_ = new(StringComparer.Ordinal);

    private string hintText_ = "按住方向键高亮，松开恢复。";

    public ManualPageViewModel()
    {
        Groups = new ObservableCollection<ManualGroupViewModel>(
        [
            new ManualGroupViewModel("belt", "皮带控制", usesDpad: false, ("up", "上"), ("down", "下")),
            new ManualGroupViewModel("angle", "角度控制", usesDpad: true, ("up", "上"), ("left", "左"), ("right", "右"), ("down", "下")),
            new ManualGroupViewModel("force", "推杆控制", usesDpad: false, ("up", "上"), ("down", "下")),
            new ManualGroupViewModel("lift", "升降控制", usesDpad: false, ("up", "上"), ("down", "下")),
        ]);
    }

    public ObservableCollection<ManualGroupViewModel> Groups { get; }

    public string HintText
    {
        get => hintText_;
        private set => SetProperty(ref hintText_, value);
    }

    public void HandleButtonPressed(ManualButtonViewModel button)
    {
        if (activeButtonsByGroup_.TryGetValue(button.GroupKey, out var previousButton) && !ReferenceEquals(previousButton, button))
        {
            previousButton.IsActive = false;
        }

        if (button.IsActive)
        {
            return;
        }

        button.IsActive = true;
        activeButtonsByGroup_[button.GroupKey] = button;
        UpdateHintText();
    }

    public void HandleButtonReleased(ManualButtonViewModel button)
    {
        if (!button.IsActive)
        {
            return;
        }

        if (activeButtonsByGroup_.TryGetValue(button.GroupKey, out var activeButton) && ReferenceEquals(activeButton, button))
        {
            activeButtonsByGroup_.Remove(button.GroupKey);
        }

        button.IsActive = false;
        UpdateHintText();
    }

    public void ReleaseAll()
    {
        foreach (var button in activeButtonsByGroup_.Values)
        {
            button.IsActive = false;
        }

        activeButtonsByGroup_.Clear();

        foreach (var group in Groups)
        {
            group.ResetActiveButtons();
        }

        HintText = "按住方向键高亮，松开恢复。";
    }

    private static string GetGroupLabel(string groupKey)
    {
        return groupKey switch
        {
            "belt" => "皮带",
            "angle" => "角度",
            "force" => "推杆",
            "lift" => "升降",
            _ => groupKey,
        };
    }

    private void UpdateHintText()
    {
        if (activeButtonsByGroup_.Count == 0)
        {
            HintText = "按住方向键高亮，松开恢复。";
            return;
        }

        var button = activeButtonsByGroup_.Values.Last();
        HintText = $"预览中: {GetGroupLabel(button.GroupKey)} {button.DisplayName}";
    }
}
