using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CM.EDITAR.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
