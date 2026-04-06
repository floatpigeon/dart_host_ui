using DartGui.App.Models;

namespace DartGui.App.ViewModels;

public sealed class StatusCardViewModel : ViewModelBase
{
    private string valueText_ = "--";
    private string unitText_ = string.Empty;
    private string detailText_ = "等待数据";
    private HealthState state_ = HealthState.Offline;

    public StatusCardViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public string ValueText
    {
        get => valueText_;
        private set => SetProperty(ref valueText_, value);
    }

    public string UnitText
    {
        get => unitText_;
        private set => SetProperty(ref unitText_, value);
    }

    public string DetailText
    {
        get => detailText_;
        private set => SetProperty(ref detailText_, value);
    }

    public HealthState State
    {
        get => state_;
        private set
        {
            if (SetProperty(ref state_, value))
            {
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(IsNormal));
                OnPropertyChanged(nameof(IsWarning));
                OnPropertyChanged(nameof(IsStale));
                OnPropertyChanged(nameof(IsOffline));
            }
        }
    }

    public string StateText => State switch
    {
        HealthState.Normal => "正常",
        HealthState.Warning => "告警",
        HealthState.Stale => "陈旧",
        _ => "失联",
    };

    public bool IsNormal => State == HealthState.Normal;

    public bool IsWarning => State == HealthState.Warning;

    public bool IsStale => State == HealthState.Stale;

    public bool IsOffline => State == HealthState.Offline;

    public void ApplyValue(string valueText, string unitText, HealthState state, string detailText)
    {
        ValueText = valueText;
        UnitText = unitText;
        State = state;
        DetailText = detailText;
    }
}
