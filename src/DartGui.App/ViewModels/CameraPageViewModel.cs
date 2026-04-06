using System.Collections.ObjectModel;

namespace DartGui.App.ViewModels;

public sealed class CameraPageViewModel : ViewModelBase
{
    public CameraPageViewModel()
    {
        Cameras = new ObservableCollection<CameraCardViewModel>(
        [
            new("工业相机 1", true, "1440x1080", "主视角预留区域"),
            new("工业相机 2", true, "1440x1080", "辅助视角预留区域"),
        ]);
    }

    public ObservableCollection<CameraCardViewModel> Cameras { get; }
}
