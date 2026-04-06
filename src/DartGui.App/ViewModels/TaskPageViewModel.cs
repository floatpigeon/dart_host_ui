using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class TaskPageViewModel : ViewModelBase
{
    private string feedbackTitle_ = "界面预览模式";
    private string feedbackMessage_ = "当前按钮仅保留布局与本地交互，不连接后台。";
    private string queueSummary_ = "当前没有排队任务。";
    private bool isFeedbackPositive_;
    private bool isFeedbackNegative_;

    public TaskPageViewModel()
    {
        Commands = new ObservableCollection<TaskCommandButtonViewModel>(
        [
            new("slider_init", "滑块初始化", SelectCommand),
            new("launch_prepare", "发射准备", SelectCommand),
            new("launch_cancel", "取消上膛", SelectCommand),
            new("fire_preload", "预装填发射", SelectCommand),
            new("cancel", "取消 / 停止", SelectCommand, isDanger: true),
            new("recover", "恢复", SelectCommand, isRecovery: true),
        ]);

        QueueItems = new ObservableCollection<TaskQueueItemViewModel>(
        [
            new TaskQueueItemViewModel(
                "当前队列为空",
                "空闲",
                "静态展示，不承载实时队列。",
                IsRunning: false,
                IsQueued: false,
                IsIdle: true)
        ]);
    }

    public ObservableCollection<TaskCommandButtonViewModel> Commands { get; }

    public ObservableCollection<TaskQueueItemViewModel> QueueItems { get; }

    public string FeedbackTitle
    {
        get => feedbackTitle_;
        private set => SetProperty(ref feedbackTitle_, value);
    }

    public string FeedbackMessage
    {
        get => feedbackMessage_;
        private set => SetProperty(ref feedbackMessage_, value);
    }

    public bool IsFeedbackPositive
    {
        get => isFeedbackPositive_;
        private set => SetProperty(ref isFeedbackPositive_, value);
    }

    public bool IsFeedbackNegative
    {
        get => isFeedbackNegative_;
        private set => SetProperty(ref isFeedbackNegative_, value);
    }

    public string QueueSummary
    {
        get => queueSummary_;
        private set => SetProperty(ref queueSummary_, value);
    }

    public void ShowStopPreview()
    {
        ClearSelection();
        SetFeedback("停止已确认", "当前为本地 UI 壳，未向外部系统发送停止命令。", false, false);
    }

    private void SelectCommand(TaskCommandButtonViewModel button)
    {
        foreach (var candidate in Commands)
        {
            candidate.SetSelected(ReferenceEquals(candidate, button));
        }

        SetFeedback("界面预览模式", $"{button.DisplayName} 仅用于布局与交互预览。", false, false);
    }

    private void ClearSelection()
    {
        foreach (var button in Commands)
        {
            button.SetSelected(false);
        }
    }

    private void SetFeedback(string title, string message, bool positive, bool negative)
    {
        FeedbackTitle = title;
        FeedbackMessage = message;
        IsFeedbackPositive = positive;
        IsFeedbackNegative = negative;
    }

}
