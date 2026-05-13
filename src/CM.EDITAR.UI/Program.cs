using Avalonia;
using Avalonia.ReactiveUI;
using CM.EDITAR.Core;

namespace CM.EDITAR.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppPaths.EnsureAll();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
