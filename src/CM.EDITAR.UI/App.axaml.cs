using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CM.EDITAR.ApplyService;
using CM.EDITAR.FileCreator;
using CM.EDITAR.Registry;
using CM.EDITAR.Templates;
using CM.EDITAR.Core;
using CM.EDITAR.UI.ViewModels;
using CM.EDITAR.UI.Views;

namespace CM.EDITAR.UI;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Composition root — no DI container needed for this MVP scope.
        var registry = new RegistryService();
        var snapshots = new SnapshotService();
        var secrets = new SecretStore();
        var audit = new AuditLog(secrets);
        var apply = new ApplyService.ApplyService(registry, snapshots, audit);
        var templates = new TemplateService();

        // Seed the starter pack on first run (idempotent: skipped when templates already exist).
        // Stored so TemplateManagerLauncher can await completion before showing the list,
        // preventing a race where the manager opens before seeding finishes on first launch.
        var starterPackReady = Task.Run(() => StarterPackImporter.RunAsync());

        var mainVm = new MainViewModel(registry, apply, snapshots, templates);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = new MainWindow { DataContext = mainVm };

            // Wire the View-side hooks the VM uses to project dialogs/windows. Keeps the VM
            // free of Avalonia types so it remains unit-testable.
            mainVm.PreflightDialogHost = async pf =>
            {
                var dlg = new PreflightDialog { DataContext = pf };
                await dlg.ShowDialog(main);
                return pf.UserAccepted && pf.TypedConfirmationValid;
            };
            mainVm.TemplateManagerLauncher = async () =>
            {
                // Await seeding so the manager always opens with a populated list on first run.
                // On subsequent launches HasExistingTemplates returns immediately, so no delay.
                // NOTE: no ConfigureAwait(false) — Avalonia requires window creation/Show on the
                // UI thread, so the continuation must resume on the captured sync context.
                await starterPackReady;
                var win = new TemplateManagerWindow { DataContext = new TemplateManagerViewModel(templates) };
                win.Show(main);
            };

            // First-run walkthrough: shown once per install. Dismissal is persisted as a marker file
            // under %LocalAppData%\CM.EDITAR+\walkthrough.seen so subsequent launches stay quiet.
            var walkthroughMarker = Path.Combine(AppPaths.Local, "walkthrough.seen");
            mainVm.WalkthroughLauncher = async void () =>
            {
                var win = new Window
                {
                    Title = "Welcome to CM.EDITAR+",
                    Width = 560, Height = 360,
                    Content = new WalkthroughOverlay(),
                };
                // Marker is written only after the user actually closes the walkthrough,
                // so an interrupted/crashed first run will re-show on next launch.
                await win.ShowDialog(main);
                try { File.WriteAllText(walkthroughMarker, DateTimeOffset.UtcNow.ToString("o")); } catch { }
            };

            desktop.MainWindow = main;
            main.Opened += (_, _) =>
            {
                if (!File.Exists(walkthroughMarker)) mainVm.ShowWalkthroughCommand.Execute(null);
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
