using DartGui.App.Models;
using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class TaskPageViewModel : ViewModelBase
{
    private readonly Func<string, CancellationToken, Task<BridgeCommandAck>> sendCommandAsync_;
    private ConnectionState connectionState_;

    private string feedbackTitle_ = "界面预览模式";
    private string feedbackMessage_ = "等待连接本地 GuiBridge。";
    private string queueSummary_ = "当前没有排队任务。";
    private bool isFeedbackPositive_;
    private bool isFeedbackNegative_;

    public TaskPageViewModel(Func<string, CancellationToken, Task<BridgeCommandAck>> sendCommandAsync)
    {
        sendCommandAsync_ = sendCommandAsync;

        Commands = new ObservableCollection<TaskCommandButtonViewModel>(
        [
            new("slider_init", "滑块初始化", SelectCommandAsync),
            new("launch_prepare", "发射准备", SelectCommandAsync),
            new("launch_cancel", "取消上膛", SelectCommandAsync),
            new("fire_preload", "预装填发射", SelectCommandAsync),
            new("cancel", "取消 / 停止", SelectCommandAsync, isDanger: true),
            new("recover", "恢复", SelectCommandAsync, isRecovery: true),
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

    public void ApplyConnectionState(ConnectionState state)
    {
        connectionState_ = state;

        foreach (var button in Commands)
        {
            button.SetEnabled(state == ConnectionState.Online);
        }

        if (state == ConnectionState.Online)
        {
            SetFeedback("已连接 GuiBridge", "任务命令将发送到本地桥接服务。", true, false);
            return;
        }

        SetFeedback("桥接离线", "当前无法发送任务命令，请检查 RMCS 内的 GuiBridge 组件。", false, true);
    }

    public void ApplySnapshot(BridgeManagerState manager)
    {
        var queuedItems = manager.Queue
            .Select(item => new TaskQueueItemViewModel(
                item.DisplayName,
                "排队中",
                item.TaskName,
                IsRunning: false,
                IsQueued: true,
                IsIdle: false))
            .ToList();

        QueueItems.Clear();
        if (queuedItems.Count == 0)
        {
            QueueItems.Add(new TaskQueueItemViewModel(
                "当前队列为空",
                "空闲",
                "没有排队任务。",
                IsRunning: false,
                IsQueued: false,
                IsIdle: true));
            QueueSummary = "当前没有排队任务。";
        }
        else
        {
            foreach (var item in queuedItems)
            {
                QueueItems.Add(item);
            }

            QueueSummary = $"当前排队 {queuedItems.Count} 项任务。";
        }

        foreach (var button in Commands)
        {
            button.SetSelected(
                string.Equals(button.CommandName, manager.CurrentTask, StringComparison.Ordinal),
                "执行中");
        }
    }

    public void ApplyCommandResult(string title, string message, bool positive, bool negative)
    {
        SetFeedback(title, message, positive, negative);
    }

    private async Task SelectCommandAsync(TaskCommandButtonViewModel button)
    {
        if (connectionState_ != ConnectionState.Online)
        {
            button.SetCommandResult(false);
            SetFeedback("桥接离线", "当前无法发送任务命令。", false, true);
            return;
        }

        foreach (var commandButton in Commands)
        {
            commandButton.ResetState();
        }

        button.SetPending();

        var ack = await sendCommandAsync_(button.CommandName, CancellationToken.None);
        button.SetCommandResult(ack.Accepted);

        if (ack.Accepted)
        {
            SetFeedback("命令已受理", $"{button.DisplayName} 已发送到 RMCS。", true, false);
            return;
        }

        SetFeedback(
            "命令发送失败",
            $"{button.DisplayName} 未执行: {ManagerDisplayText.ReasonText(ack.Reason)}。",
            false,
            true);
    }

    private void SetFeedback(string title, string message, bool positive, bool negative)
    {
        FeedbackTitle = title;
        FeedbackMessage = message;
        IsFeedbackPositive = positive;
        IsFeedbackNegative = negative;
    }
}
