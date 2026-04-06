using System.Collections.ObjectModel;
using DartGui.App.Models;

namespace DartGui.App.ViewModels;

public sealed class KeyStatusPanelViewModel : ViewModelBase
{
    public KeyStatusPanelViewModel()
    {
        Cards = new ObservableCollection<StatusCardViewModel>(
        [
            CreateCard("任务概览", "IDLE", string.Empty, HealthState.Normal, "本地预览模式"),
            CreateCard("当前动作", "空闲", string.Empty, HealthState.Normal, "等待界面操作"),
            CreateCard("Pitch", "0.00", "deg/s", HealthState.Normal, "静态样例值"),
            CreateCard("力传感器 CH1", "24.80", "kg", HealthState.Normal, "静态样例值"),
            CreateCard("力传感器 CH2", "25.20", "kg", HealthState.Normal, "静态样例值"),
        ]);
    }

    public ObservableCollection<StatusCardViewModel> Cards { get; }

    private static StatusCardViewModel CreateCard(
        string title,
        string value,
        string unit,
        HealthState state,
        string detail)
    {
        var card = new StatusCardViewModel(title);
        card.ApplyValue(value, unit, state, detail);
        return card;
    }
}
