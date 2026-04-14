using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DartHost.App.Services;
using DartHost.App.ViewModels;
using DartHost.App.Views;

namespace DartHost.App;

public partial class App : Application
{
    private IHostConnectionService? _connectionService;
    private MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _connectionService = new HostConnectionService();
        _mainWindowViewModel = new MainWindowViewModel(_connectionService);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _mainWindowViewModel
                };
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView
                {
                    DataContext = _mainWindowViewModel
                };
                break;
        }

        if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
        {
            controlledLifetime.Exit += OnExit;
        }

        _ = _connectionService.StartAsync();

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _mainWindowViewModel?.Dispose();
        if (_connectionService is not null)
        {
            _connectionService.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
