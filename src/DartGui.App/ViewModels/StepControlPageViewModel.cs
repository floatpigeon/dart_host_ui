using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class StepControlPageViewModel : ViewModelBase
{
    public StepControlPageViewModel()
    {
        Commands = new ObservableCollection<TaskCommandButtonViewModel>(
        [
            CreateCommand("step_a", "步骤 A"),
            CreateCommand("step_b", "步骤 B"),
            CreateCommand("step_c", "步骤 C"),
            CreateCommand("step_d", "步骤 D"),
            CreateCommand("step_e", "步骤 E"),
            CreateCommand("step_f", "步骤 F"),
        ]);
    }

    public ObservableCollection<TaskCommandButtonViewModel> Commands { get; }

    public string HintTitle => "单步控制";

    public string HintMessage
    {
        get => hintMessage_;
        private set => SetProperty(ref hintMessage_, value);
    }

    private string hintMessage_ = "单步按钮仅用于展示版式与本地高亮反馈。";

    public void ClearSelection()
    {
        foreach (var button in Commands)
        {
            button.SetSelected(false);
        }
    }

    private void SelectCommand(TaskCommandButtonViewModel button)
    {
        foreach (var candidate in Commands)
        {
            candidate.SetSelected(ReferenceEquals(candidate, button));
        }

        HintMessage = $"{button.DisplayName} 仅用于单步控制区的界面预览。";
    }

    private TaskCommandButtonViewModel CreateCommand(string commandName, string displayName)
    {
        return new TaskCommandButtonViewModel(
            commandName,
            displayName,
            button =>
            {
                SelectCommand(button);
                return Task.CompletedTask;
            });
    }
}
