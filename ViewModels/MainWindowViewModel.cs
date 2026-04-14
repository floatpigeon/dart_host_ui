using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DartHost.App.Models;
using DartHost.App.Services;
using DartHost.App.State;

namespace DartHost.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IHostConnectionService _connectionService;

    [ObservableProperty]
    private AppShellState _appShellState = AppShellState.Default;

    [ObservableProperty]
    private ManagerStatusState _managerStatusState = ManagerStatusState.Empty;

    [ObservableProperty]
    private FeatureAvailabilityState _featureAvailabilityState = FeatureAvailabilityState.Default;

    public MainWindowViewModel(IHostConnectionService connectionService)
    {
        _connectionService = connectionService;

        TaskCommands =
        [
            CreateCommandItem(
                "launch_prepare",
                "发射准备",
                ""),
            CreateCommandItem(
                "launch_cancel",
                "取消发射",
                ""),
            CreateCommandItem(
                "fire_preload",
                "预装填发射",
                "")
        ];

        QueueItems = new ObservableCollection<QueueItemState>();
        SupportedCommands = new ReadOnlyObservableCollection<CommandItemViewModel>(TaskCommands);

        ShowTaskControlCommand = new RelayCommand(() => SelectLeftPanel(LeftPanelSelection.TaskControl));
        ShowStepControlCommand = new RelayCommand(() => SelectLeftPanel(LeftPanelSelection.StepControl));
        ShowManualControlCommand = new RelayCommand(() => SelectLeftPanel(LeftPanelSelection.ManualControl));
        ShowDeviceStatusCommand = new RelayCommand(() => SelectRightPanel(RightPanelSelection.DeviceStatus));
        ShowVisualFeedbackCommand = new RelayCommand(() => SelectRightPanel(RightPanelSelection.VisualFeedback));

        RecoverCommand = new AsyncRelayCommand(() => SendCommandAsync("recover"), CanRecover);
        CancelCommand = new AsyncRelayCommand(() => SendCommandAsync("cancel"), CanCancel);

        _connectionService.StateChanged += OnConnectionServiceStateChanged;
        RefreshFromService();
    }

    public ObservableCollection<CommandItemViewModel> TaskCommands { get; }

    public ReadOnlyObservableCollection<CommandItemViewModel> SupportedCommands { get; }

    public ObservableCollection<QueueItemState> QueueItems { get; }

    public IRelayCommand ShowTaskControlCommand { get; }

    public IRelayCommand ShowStepControlCommand { get; }

    public IRelayCommand ShowManualControlCommand { get; }

    public IRelayCommand ShowDeviceStatusCommand { get; }

    public IRelayCommand ShowVisualFeedbackCommand { get; }

    public IAsyncRelayCommand RecoverCommand { get; }

    public IAsyncRelayCommand CancelCommand { get; }

    public bool HasGlobalError => !string.IsNullOrWhiteSpace(AppShellState.GlobalErrorMessage);

    public bool IsTaskControlSelected => AppShellState.LeftPanel == LeftPanelSelection.TaskControl;

    public bool IsStepControlSelected => AppShellState.LeftPanel == LeftPanelSelection.StepControl;

    public bool IsManualControlSelected => AppShellState.LeftPanel == LeftPanelSelection.ManualControl;

    public bool IsDeviceStatusSelected => AppShellState.RightPanel == RightPanelSelection.DeviceStatus;

    public bool IsVisualFeedbackSelected => AppShellState.RightPanel == RightPanelSelection.VisualFeedback;

    public string ConnectionStatusText => AppShellState.ConnectionStatus switch
    {
        HostConnectionStatus.Connected => "已连接",
        HostConnectionStatus.Connecting => "连接中",
        HostConnectionStatus.Faulted => "故障",
        _ => "未连接"
    };

    public string ConnectionBadgeBrush => AppShellState.ConnectionStatus switch
    {
        HostConnectionStatus.Connected => "#2D9F67",
        HostConnectionStatus.Connecting => "#D97706",
        HostConnectionStatus.Faulted => "#C2410C",
        _ => "#475569"
    };

    public string LifecycleText => string.IsNullOrWhiteSpace(ManagerStatusState.LifecycleState)
        ? "未知"
        : ManagerStatusState.LifecycleState;

    public string CurrentTaskText => string.IsNullOrWhiteSpace(ManagerStatusState.CurrentTask)
        ? "空闲"
        : ManagerStatusState.CurrentTask;

    public string CurrentActionText => string.IsNullOrWhiteSpace(ManagerStatusState.CurrentAction)
        ? "等待"
        : ManagerStatusState.CurrentAction;

    public string FireCountText => ManagerStatusState.FireCount.ToString();

    public bool ShowEmptyQueue => QueueItems.Count == 0;

    public string LastManagerErrorText => ManagerStatusState.LastError is null
        ? "无错误"
        : $"{ManagerStatusState.LastError.TaskName} / {ManagerStatusState.LastError.ActionName}: {ManagerStatusState.LastError.Message} ({ManagerStatusState.LastError.Reason})";

    public string PitchValueText => FeatureAvailabilityState.DeviceRawStateAvailable ? "0.0 deg" : "--";

    public string ForceChannel1Text => FeatureAvailabilityState.DeviceRawStateAvailable ? "0.00 N" : "--";

    public string ForceChannel2Text => FeatureAvailabilityState.DeviceRawStateAvailable ? "0.00 N" : "--";

    public void Dispose()
    {
        _connectionService.StateChanged -= OnConnectionServiceStateChanged;
    }

    partial void OnAppShellStateChanged(AppShellState value)
    {
        OnPropertyChanged(nameof(HasGlobalError));
        OnPropertyChanged(nameof(IsTaskControlSelected));
        OnPropertyChanged(nameof(IsStepControlSelected));
        OnPropertyChanged(nameof(IsManualControlSelected));
        OnPropertyChanged(nameof(IsDeviceStatusSelected));
        OnPropertyChanged(nameof(IsVisualFeedbackSelected));
        OnPropertyChanged(nameof(ConnectionStatusText));
        OnPropertyChanged(nameof(ConnectionBadgeBrush));
        RecoverCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnManagerStatusStateChanged(ManagerStatusState value)
    {
        OnPropertyChanged(nameof(LifecycleText));
        OnPropertyChanged(nameof(CurrentTaskText));
        OnPropertyChanged(nameof(CurrentActionText));
        OnPropertyChanged(nameof(FireCountText));
        OnPropertyChanged(nameof(LastManagerErrorText));
    }

    partial void OnFeatureAvailabilityStateChanged(FeatureAvailabilityState value)
    {
        OnPropertyChanged(nameof(PitchValueText));
        OnPropertyChanged(nameof(ForceChannel1Text));
        OnPropertyChanged(nameof(ForceChannel2Text));
    }

    private CommandItemViewModel CreateCommandItem(
        string name,
        string displayName,
        string description)
    {
        return new CommandItemViewModel(
            new CommandDefinition(name, displayName, description),
            () => SendCommandAsync(name));
    }

    private void OnConnectionServiceStateChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshFromService();
            return;
        }

        Dispatcher.UIThread.Post(RefreshFromService);
    }

    private void RefreshFromService()
    {
        AppShellState = AppShellState with
        {
            ConnectionStatus = _connectionService.ConnectionStatus,
            GlobalErrorMessage = _connectionService.GlobalErrorMessage,
            ConnectionDetail = _connectionService.ConnectionDetail,
            SessionId = _connectionService.SessionId,
            LastHeartbeatUtc = _connectionService.LastHeartbeatUtc
        };

        ManagerStatusState = _connectionService.ManagerStatus;
        FeatureAvailabilityState = _connectionService.FeatureAvailability;

        QueueItems.Clear();
        foreach (var item in ManagerStatusState.Queue)
        {
            QueueItems.Add(item);
        }

        OnPropertyChanged(nameof(ShowEmptyQueue));
        UpdateCommandAvailability();
    }

    private void UpdateCommandAvailability()
    {
        var isConnected = AppShellState.ConnectionStatus == HostConnectionStatus.Connected;
        var supportedCommands = _connectionService.SupportedCommands;

        foreach (var command in TaskCommands)
        {
            command.IsEnabled =
                FeatureAvailabilityState.TaskControlAvailable
                && isConnected
                && supportedCommands.Contains(command.Definition.Name);
        }

        RecoverCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private void SelectLeftPanel(LeftPanelSelection selection)
    {
        AppShellState = AppShellState with { LeftPanel = selection };
    }

    private void SelectRightPanel(RightPanelSelection selection)
    {
        AppShellState = AppShellState with { RightPanel = selection };
    }

    private bool CanRecover()
    {
        return AppShellState.ConnectionStatus == HostConnectionStatus.Connected
            && _connectionService.SupportedCommands.Contains("recover");
    }

    private bool CanCancel()
    {
        return AppShellState.ConnectionStatus == HostConnectionStatus.Connected
            && _connectionService.SupportedCommands.Contains("cancel");
    }

    private async Task SendCommandAsync(string commandName)
    {
        await _connectionService.SendCommandAsync(commandName);
    }
}
