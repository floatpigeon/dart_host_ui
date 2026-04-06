using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DartGui.App.ViewModels;

namespace DartGui.App.Views;

public partial class ManualPageView : UserControl
{
    public ManualPageView()
    {
        InitializeComponent();
    }

    private void ManualButtonOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not ManualPageViewModel viewModel ||
            sender is not Button { Tag: ManualButtonViewModel button })
        {
            return;
        }

        viewModel.HandleButtonPressed(button);
        e.Handled = true;
    }

    private void ManualButtonOnPointerReleased(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ManualPageViewModel viewModel ||
            sender is not Button { Tag: ManualButtonViewModel button })
        {
            return;
        }

        viewModel.HandleButtonReleased(button);
        e.Handled = true;
    }
}
