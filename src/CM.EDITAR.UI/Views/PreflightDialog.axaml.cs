using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CM.EDITAR.UI.ViewModels;

namespace CM.EDITAR.UI.Views;

public partial class PreflightDialog : Window
{
    public PreflightDialog() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnAcceptClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PreflightViewModel vm)
        {
            // Block accept if a typed confirmation is required and not satisfied.
            if (vm.Manifest.RequiresTypedConfirmation && !vm.TypedConfirmationValid) return;
            vm.UserAccepted = true;
        }
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PreflightViewModel vm) vm.UserAccepted = false;
        Close();
    }
}
