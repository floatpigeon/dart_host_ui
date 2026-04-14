using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DartHost.App.Models;

namespace DartHost.App.ViewModels;

public sealed partial class CommandItemViewModel : ObservableObject
{
    private readonly Func<Task> _executeAsync;

    [ObservableProperty]
    private bool _isEnabled;

    public CommandItemViewModel(CommandDefinition definition, Func<Task> executeAsync)
    {
        Definition = definition;
        _executeAsync = executeAsync;
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    public CommandDefinition Definition { get; }

    public string Title => Definition.DisplayName;

    public IAsyncRelayCommand ExecuteCommand { get; }

    private async Task ExecuteAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        await _executeAsync();
    }
}
