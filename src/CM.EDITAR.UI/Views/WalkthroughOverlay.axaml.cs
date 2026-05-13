using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace CM.EDITAR.UI.Views;

public partial class WalkthroughOverlay : UserControl
{
    public WalkthroughOverlay() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnGotItClick(object? sender, RoutedEventArgs e)
    {
        // Walk up the visual tree to the host Window and close it. The App composition
        // root awaits ShowDialog and writes the "walkthrough.seen" marker on close.
        if (this.GetVisualRoot() is Window w) w.Close();
    }
}
