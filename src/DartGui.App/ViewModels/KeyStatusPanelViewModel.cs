using System.Collections.ObjectModel;
using DartGui.App.Models;

namespace DartGui.App.ViewModels;

public sealed class KeyStatusPanelViewModel : ViewModelBase
{
    private readonly StatusCardViewModel lifecycleCard_;
    private readonly StatusCardViewModel actionCard_;
    private readonly StatusCardViewModel fireCountCard_;
    private readonly StatusCardViewModel beltCard_;
    private readonly StatusCardViewModel liftCard_;

    public KeyStatusPanelViewModel()
    {
        lifecycleCard_ = CreateCard("任务概览", "离线", string.Empty, HealthState.Offline, "等待 GuiBridge");
        actionCard_ = CreateCard("当前动作", "空闲", string.Empty, HealthState.Offline, "等待 GuiBridge");
        fireCountCard_ = CreateCard("发射计数", "--", string.Empty, HealthState.Offline, "等待 GuiBridge");
        beltCard_ = CreateCard("皮带反馈", "--", string.Empty, HealthState.Offline, "等待 GuiBridge");
        liftCard_ = CreateCard("升降反馈", "--", string.Empty, HealthState.Offline, "等待 GuiBridge");

        Cards = new ObservableCollection<StatusCardViewModel>(
        [
            lifecycleCard_,
            actionCard_,
            fireCountCard_,
            beltCard_,
            liftCard_,
        ]);
    }

    public ObservableCollection<StatusCardViewModel> Cards { get; }

    public void ApplySnapshot(BridgeManagerState manager, BridgeFeedbackState feedback)
    {
        lifecycleCard_.ApplyValue(
            manager.LifecycleState,
            string.Empty,
            HealthState.Normal,
            $"当前任务: {ManagerDisplayText.TaskName(manager.CurrentTask)}");

        actionCard_.ApplyValue(
            ManagerDisplayText.ActionName(manager.CurrentAction),
            string.Empty,
            HealthState.Normal,
            $"队列长度: {manager.Queue.Count}");

        fireCountCard_.ApplyValue(
            manager.FireCount.ToString(),
            string.Empty,
            HealthState.Normal,
            "累计完成 fire_preload 次数");

        beltCard_.ApplyValue(
            $"L {feedback.Belt.LeftVelocity:F2} / R {feedback.Belt.RightVelocity:F2}",
            string.Empty,
            HealthState.Normal,
            $"Tq {feedback.Belt.LeftTorque:F2} / {feedback.Belt.RightTorque:F2}");

        liftCard_.ApplyValue(
            $"L {feedback.Lift.LeftVelocity:F2} / R {feedback.Lift.RightVelocity:F2}",
            string.Empty,
            HealthState.Normal,
            "升降左右速度反馈");
    }

    public void ShowDisconnected()
    {
        lifecycleCard_.ApplyValue("离线", string.Empty, HealthState.Offline, "等待 GuiBridge");
        actionCard_.ApplyValue("空闲", string.Empty, HealthState.Offline, "等待 GuiBridge");
        fireCountCard_.ApplyValue("--", string.Empty, HealthState.Offline, "等待 GuiBridge");
        beltCard_.ApplyValue("--", string.Empty, HealthState.Offline, "等待 GuiBridge");
        liftCard_.ApplyValue("--", string.Empty, HealthState.Offline, "等待 GuiBridge");
    }

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
