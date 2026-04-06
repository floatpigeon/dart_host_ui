using DartGui.App.Models;

namespace DartGui.App.ViewModels;

public sealed class ConnectionBadgeViewModel : ViewModelBase
{
    private ConnectionState state_;

    public ConnectionBadgeViewModel(string label)
    {
        Label = label;
    }

    public string Label { get; }

    public ConnectionState State
    {
        get => state_;
        private set
        {
            if (SetProperty(ref state_, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(IsReconnecting));
                OnPropertyChanged(nameof(IsOffline));
            }
        }
    }

    public string StatusText => State switch
    {
        ConnectionState.Online => "在线",
        ConnectionState.Reconnecting => "重连中",
        _ => "离线",
    };

    public bool IsOnline => State == ConnectionState.Online;

    public bool IsReconnecting => State == ConnectionState.Reconnecting;

    public bool IsOffline => State == ConnectionState.Offline;

    public void SetState(ConnectionState state)
    {
        State = state;
    }
}
