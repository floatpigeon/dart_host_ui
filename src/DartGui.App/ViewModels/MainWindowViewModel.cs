using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DartGui.App.Models;

namespace DartGui.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase currentPage_;
    private bool isFaultPanelOpen_;
    private bool isStopDialogOpen_;
    private string currentTaskText_ = "空闲";
    private string managerStateText_ = "IDLE";
    private string faultSummary_ = "无告警";
    private bool hasActiveFault_;

    public MainWindowViewModel()
    {
        GuiBridgeBadge = new ConnectionBadgeViewModel("界面");
        RmcsBadge = new ConnectionBadgeViewModel("状态");
        GuiBridgeBadge.SetState(ConnectionState.Online);
        RmcsBadge.SetState(ConnectionState.Online);

        Faults = new ObservableCollection<FaultEntryViewModel>(
        [
            new FaultEntryViewModel(
                "--:--:--",
                "本地预览",
                "ui-shell",
                "当前没有故障记录",
                "静态展示",
                "提示")
        ]);

        TaskPage = new TaskPageViewModel();
        ManualPage = new ManualPageViewModel();
        StepControlPage = new StepControlPageViewModel();
        CameraPage = new CameraPageViewModel();
        KeyStatusPanel = new KeyStatusPanelViewModel();
        currentPage_ = TaskPage;

        ShowTaskPageCommand = new RelayCommand(() => CurrentPage = TaskPage);
        ShowManualPageCommand = new RelayCommand(() => CurrentPage = ManualPage);
        ShowStepControlPageCommand = new RelayCommand(() => CurrentPage = StepControlPage);
        ShowCameraPageCommand = new RelayCommand(() => CurrentPage = CameraPage);
        ToggleFaultPanelCommand = new RelayCommand(() => IsFaultPanelOpen = !IsFaultPanelOpen);
        RequestStopCommand = new RelayCommand(() => IsStopDialogOpen = true);
        CancelStopCommand = new RelayCommand(() => IsStopDialogOpen = false);
        ConfirmStopCommand = new RelayCommand(ConfirmStop);
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

    public IRelayCommand ConfirmStopCommand { get; }

    public void HandleWindowDeactivated()
    {
        ManualPage.ReleaseAll();
    }

    private void ConfirmStop()
    {
        IsStopDialogOpen = false;
        CurrentTaskText = "空闲";
        ManagerStateText = "IDLE";
        FaultSummary = "无告警";
        HasActiveFault = false;
        TaskPage.ShowStopPreview();
        StepControlPage.ClearSelection();
        ManualPage.ReleaseAll();
    }
}
