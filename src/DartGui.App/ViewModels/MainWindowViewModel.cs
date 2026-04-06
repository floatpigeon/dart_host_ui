using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DartGui.App.Models;
using DartGui.App.Services;

namespace DartGui.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const string DefaultGuiBridgeUrl = "ws://127.0.0.1:18081/ws";

    private readonly GuiBridgeClient bridgeClient_;
    private ViewModelBase currentPage_;
    private bool isFaultPanelOpen_;
    private bool isStopDialogOpen_;
    private string currentTaskText_ = "空闲";
    private string managerStateText_ = "离线";
    private string faultSummary_ = "无告警";
    private bool hasActiveFault_;
    private BridgeFaultInfo? activeFault_;
    private BridgeFaultInfo? pendingRecoveredFault_;

    public MainWindowViewModel()
    {
        GuiBridgeBadge = new ConnectionBadgeViewModel("界面");
        RmcsBadge = new ConnectionBadgeViewModel("状态");
        GuiBridgeBadge.SetState(ConnectionState.Offline);
        RmcsBadge.SetState(ConnectionState.Offline);

        Faults = new ObservableCollection<FaultEntryViewModel>(
        [
            new FaultEntryViewModel(
                "--:--:--",
                "GuiBridge",
                "waiting",
                "当前没有故障记录",
                "等待 RMCS 推送",
                "提示")
        ]);

        TaskPage = new TaskPageViewModel(SendTaskCommandAsync);
        TaskPage.ApplyConnectionState(ConnectionState.Offline);
        ManualPage = new ManualPageViewModel();
        StepControlPage = new StepControlPageViewModel();
        CameraPage = new CameraPageViewModel();
        KeyStatusPanel = new KeyStatusPanelViewModel();
        currentPage_ = TaskPage;

        bridgeClient_ = new GuiBridgeClient(ResolveGuiBridgeEndpoint());
        bridgeClient_.ConnectionStateChanged += OnConnectionStateChanged;
        bridgeClient_.SnapshotReceived += OnSnapshotReceived;
        bridgeClient_.FaultEventReceived += OnFaultEventReceived;
        bridgeClient_.FaultClearedReceived += OnFaultClearedReceived;

        ShowTaskPageCommand = new RelayCommand(() => CurrentPage = TaskPage);
        ShowManualPageCommand = new RelayCommand(() => CurrentPage = ManualPage);
        ShowStepControlPageCommand = new RelayCommand(() => CurrentPage = StepControlPage);
        ShowCameraPageCommand = new RelayCommand(() => CurrentPage = CameraPage);
        ToggleFaultPanelCommand = new RelayCommand(() => IsFaultPanelOpen = !IsFaultPanelOpen);
        RequestStopCommand = new RelayCommand(() => IsStopDialogOpen = true);
        CancelStopCommand = new RelayCommand(() => IsStopDialogOpen = false);
        ConfirmStopCommand = new AsyncRelayCommand(ConfirmStopAsync);
    }

    public string Title => "DART 控制界面";

    public TaskPageViewModel TaskPage { get; }

    public ManualPageViewModel ManualPage { get; }

    public StepControlPageViewModel StepControlPage { get; }

    public CameraPageViewModel CameraPage { get; }

    public KeyStatusPanelViewModel KeyStatusPanel { get; }

    public ViewModelBase CurrentPage
    {
        get => currentPage_;
        private set
        {
            if (SetProperty(ref currentPage_, value))
            {
                OnPropertyChanged(nameof(IsTaskPageSelected));
                OnPropertyChanged(nameof(IsManualPageSelected));
                OnPropertyChanged(nameof(IsStepControlPageSelected));
                OnPropertyChanged(nameof(IsCameraPageSelected));
            }
        }
    }

    public bool IsTaskPageSelected => ReferenceEquals(CurrentPage, TaskPage);

    public bool IsManualPageSelected => ReferenceEquals(CurrentPage, ManualPage);

    public bool IsStepControlPageSelected => ReferenceEquals(CurrentPage, StepControlPage);

    public bool IsCameraPageSelected => ReferenceEquals(CurrentPage, CameraPage);

    public ConnectionBadgeViewModel GuiBridgeBadge { get; }

    public ConnectionBadgeViewModel RmcsBadge { get; }

    public ObservableCollection<FaultEntryViewModel> Faults { get; }

    public string CurrentTaskText
    {
        get => currentTaskText_;
        private set => SetProperty(ref currentTaskText_, value);
    }

    public string ManagerStateText
    {
        get => managerStateText_;
        private set => SetProperty(ref managerStateText_, value);
    }

    public string FaultSummary
    {
        get => faultSummary_;
        private set => SetProperty(ref faultSummary_, value);
    }

    public bool HasActiveFault
    {
        get => hasActiveFault_;
        private set => SetProperty(ref hasActiveFault_, value);
    }

    public bool IsFaultPanelOpen
    {
        get => isFaultPanelOpen_;
        set => SetProperty(ref isFaultPanelOpen_, value);
    }

    public bool IsStopDialogOpen
    {
        get => isStopDialogOpen_;
        private set => SetProperty(ref isStopDialogOpen_, value);
    }

    public IRelayCommand ShowTaskPageCommand { get; }

    public IRelayCommand ShowManualPageCommand { get; }

    public IRelayCommand ShowStepControlPageCommand { get; }

    public IRelayCommand ShowCameraPageCommand { get; }

    public IRelayCommand ToggleFaultPanelCommand { get; }

    public IRelayCommand RequestStopCommand { get; }

    public IRelayCommand CancelStopCommand { get; }

    public IAsyncRelayCommand ConfirmStopCommand { get; }

    public void HandleWindowDeactivated()
    {
        ManualPage.ReleaseAll();
    }

    public void Dispose()
    {
        bridgeClient_.ConnectionStateChanged -= OnConnectionStateChanged;
        bridgeClient_.SnapshotReceived -= OnSnapshotReceived;
        bridgeClient_.FaultEventReceived -= OnFaultEventReceived;
        bridgeClient_.FaultClearedReceived -= OnFaultClearedReceived;
        bridgeClient_.Dispose();
    }

    private Task<BridgeCommandAck> SendTaskCommandAsync(string command, CancellationToken cancellationToken)
    {
        return bridgeClient_.SendTaskCommandAsync(command, cancellationToken);
    }

    private static Uri ResolveGuiBridgeEndpoint()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("DART_GUI_BRIDGE_URL");
        if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var endpoint)
            && (endpoint.Scheme == Uri.UriSchemeWs || endpoint.Scheme == Uri.UriSchemeWss))
        {
            return endpoint;
        }

        return new Uri(DefaultGuiBridgeUrl);
    }

    private async Task ConfirmStopAsync()
    {
        IsStopDialogOpen = false;
        StepControlPage.ClearSelection();
        ManualPage.ReleaseAll();

        var ack = await SendTaskCommandAsync("cancel", CancellationToken.None);
        if (ack.Accepted)
        {
            TaskPage.ApplyCommandResult("停止命令已发送", "cancel 已发送到 RMCS。", true, false);
            return;
        }

        TaskPage.ApplyCommandResult(
            "停止命令发送失败",
            $"cancel 未执行: {ManagerDisplayText.ReasonText(ack.Reason)}。",
            false,
            true);
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        Dispatcher.UIThread.Post(() => ApplyConnectionState(state));
    }

    private void OnSnapshotReceived(BridgeStateSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() => ApplySnapshot(snapshot));
    }

    private void OnFaultEventReceived(BridgeFaultInfo fault)
    {
        Dispatcher.UIThread.Post(() => ApplyFaultEvent(fault));
    }

    private void OnFaultClearedReceived(long timestampMs)
    {
        Dispatcher.UIThread.Post(() => ApplyFaultCleared(timestampMs, appendEntry: true));
    }

    private void ApplyConnectionState(ConnectionState state)
    {
        GuiBridgeBadge.SetState(state);
        TaskPage.ApplyConnectionState(state);

        if (state == ConnectionState.Online)
        {
            return;
        }

        RmcsBadge.SetState(state == ConnectionState.Reconnecting ? ConnectionState.Reconnecting : ConnectionState.Offline);
        CurrentTaskText = "空闲";
        ManagerStateText = "离线";
        KeyStatusPanel.ShowDisconnected();
    }

    private void ApplySnapshot(BridgeStateSnapshot snapshot)
    {
        RmcsBadge.SetState(ConnectionState.Online);
        CurrentTaskText = ManagerDisplayText.TaskName(snapshot.Manager.CurrentTask);
        ManagerStateText = string.IsNullOrWhiteSpace(snapshot.Manager.LifecycleState)
            ? "未知"
            : snapshot.Manager.LifecycleState;

        TaskPage.ApplySnapshot(snapshot.Manager);
        KeyStatusPanel.ApplySnapshot(snapshot.Manager, snapshot.Feedback);

        if (snapshot.Manager.LastError is not null)
        {
            ApplyFaultEvent(snapshot.Manager.LastError);
        }
        else if (HasActiveFault)
        {
            ClearActiveFaultState();
        }
    }

    private void ApplyFaultEvent(BridgeFaultInfo fault)
    {
        if (activeFault_ is not null && activeFault_.TimestampMs == fault.TimestampMs)
        {
            return;
        }

        activeFault_ = fault;
        pendingRecoveredFault_ = null;
        RemoveFaultPlaceholder();

        Faults.Insert(0, new FaultEntryViewModel(
            ManagerDisplayText.TimestampText(fault.TimestampMs),
            BuildFaultSource(fault),
            string.IsNullOrWhiteSpace(fault.Reason) ? "unknown" : fault.Reason,
            BuildFaultMessage(fault),
            "等待恢复",
            "故障",
            isError: true));

        FaultSummary = BuildFaultMessage(fault);
        HasActiveFault = true;
        IsFaultPanelOpen = true;
    }

    private void ApplyFaultCleared(long timestampMs, bool appendEntry)
    {
        var recoveredFault = activeFault_ ?? pendingRecoveredFault_;
        if (appendEntry && recoveredFault is not null)
        {
            RemoveFaultPlaceholder();
            Faults.Insert(0, new FaultEntryViewModel(
                ManagerDisplayText.TimestampText(timestampMs),
                BuildFaultSource(recoveredFault),
                "recover",
                $"{BuildFaultMessage(recoveredFault)} 已恢复",
                "RMCS 已清除故障状态",
                "恢复",
                isRecovered: true));
        }

        pendingRecoveredFault_ = null;
        activeFault_ = null;
        HasActiveFault = false;
        FaultSummary = "无告警";
    }

    private void ClearActiveFaultState()
    {
        pendingRecoveredFault_ = activeFault_;
        activeFault_ = null;
        HasActiveFault = false;
        FaultSummary = "无告警";
    }

    private void RemoveFaultPlaceholder()
    {
        if (Faults.Count == 1 && Faults[0].Code == "waiting")
        {
            Faults.Clear();
        }
    }

    private static string BuildFaultSource(BridgeFaultInfo fault)
    {
        var taskText = ManagerDisplayText.TaskName(fault.TaskName);
        var actionText = ManagerDisplayText.ActionName(fault.ActionName);

        if (!string.IsNullOrWhiteSpace(fault.TaskName) && !string.IsNullOrWhiteSpace(fault.ActionName))
        {
            return $"{taskText} / {actionText}";
        }

        if (!string.IsNullOrWhiteSpace(fault.TaskName))
        {
            return taskText;
        }

        if (!string.IsNullOrWhiteSpace(fault.ActionName))
        {
            return actionText;
        }

        return "RMCS";
    }

    private static string BuildFaultMessage(BridgeFaultInfo fault)
    {
        if (!string.IsNullOrWhiteSpace(fault.Message))
        {
            return fault.Message;
        }

        return ManagerDisplayText.ReasonText(fault.Reason);
    }
}
