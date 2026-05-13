using System.Collections.ObjectModel;
using CM.EDITAR.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CM.EDITAR.UI.ViewModels;

/// <summary>
/// Main shell ViewModel. Hosts categories list, DataGrid of discovered entries, staging queue,
/// and the four right-panel cards (Selected Entry, Custom Add, Runtime Status, NewPlus).
/// Footer commands: Preflight, Apply, UndoLast, UndoAll, Flush, Export, Import.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IRegistryService _registry;
    private readonly IApplyService _apply;
    private readonly ISnapshotService _snapshots;
    private readonly ITemplateService _templates;

    public ObservableCollection<string> Categories { get; } = new()
    {
        "All Extensions", "Extension Packs", "Power User", "Text/Data",
        "Office/Docs", "Archives", "Cloud Docs", "CAD/3D",
        "Automation/AI", "Legacy", "Media", "System", "Custom",
    };

    public ObservableCollection<ShellNewEntryViewModel> Entries { get; } = new();
    public ObservableCollection<ShellNewEntryViewModel> StagingQueue { get; } = new();

    [ObservableProperty] private string _selectedCategory = "All Extensions";
    [ObservableProperty] private ShellNewEntryViewModel? _selectedEntry;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Ready.";

    /// <summary>Hook the View sets to project the Preflight dialog. Returns true if the user confirmed.</summary>
    public Func<PreflightViewModel, Task<bool>>? PreflightDialogHost { get; set; }
    /// <summary>Hook the View sets to launch the Template Manager window.</summary>
    public Func<Task>? TemplateManagerLauncher { get; set; }
    /// <summary>Hook the View sets to display the first-run walkthrough overlay.</summary>
    public Action? WalkthroughLauncher { get; set; }

    // Custom Add card
    [ObservableProperty] private string _customPackName = "Custom";
    [ObservableProperty] private string _customExtension = "";
    [ObservableProperty] private string _customDisplayName = "";

    // Runtime Status card
    [ObservableProperty] private bool _elevationRequired;       // always false at runtime
    [ObservableProperty] private int _visibleCount;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _undoCount;

    public MainViewModel(IRegistryService registry, IApplyService apply, ISnapshotService snapshots, ITemplateService templates)
    {
        _registry = registry; _apply = apply; _snapshots = snapshots; _templates = templates;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true; StatusMessage = "Discovering ShellNew entries...";
        try
        {
            Entries.Clear();
            var discovered = await _registry.DiscoverAsync().ConfigureAwait(true);
            foreach (var e in discovered) Entries.Add(new ShellNewEntryViewModel(e));
            VisibleCount = Entries.Count(e => e.Entry.Visible);
            UndoCount = (await _snapshots.ListAsync().ConfigureAwait(true)).Count;
            StatusMessage = $"Discovered {Entries.Count} entries.";
        }
        catch (Exception ex) { StatusMessage = $"Discovery failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void StageEnable() => StageAction(StagedAction.Enable);

    [RelayCommand]
    private void StageDisable() => StageAction(StagedAction.Disable);

    private void StageAction(StagedAction action)
    {
        if (SelectedEntry is null) return;
        SelectedEntry.IsStaged = true;
        SelectedEntry.StagedAction = action;
        if (!StagingQueue.Contains(SelectedEntry))
        {
            SelectedEntry.QueuePosition = StagingQueue.Count + 1;
            StagingQueue.Add(SelectedEntry);
        }
        PendingCount = StagingQueue.Count;
    }

    [RelayCommand]
    private void AddCustom()
    {
        if (string.IsNullOrWhiteSpace(CustomExtension)) return;
        var ext = CustomExtension.StartsWith('.') ? CustomExtension : "." + CustomExtension;
        var newEntry = new ShellNewEntry
        {
            Extension = ext,
            DisplayName = string.IsNullOrWhiteSpace(CustomDisplayName) ? $"New {ext} File" : CustomDisplayName,
            ShellNewType = ShellNewType.NullFile,
            SourceKey = $@"HKCU\Software\Classes\{ext}\ShellNew",
            Pack = CustomPackName,
            Group = "Custom",
        };
        var vm = new ShellNewEntryViewModel(newEntry) { IsStaged = true, StagedAction = StagedAction.Add, QueuePosition = StagingQueue.Count + 1 };
        Entries.Add(vm);
        StagingQueue.Add(vm);
        PendingCount = StagingQueue.Count;
        CustomExtension = ""; CustomDisplayName = "";
        StatusMessage = $"Staged custom add for {ext}.";
    }

    [RelayCommand]
    private async Task PreflightAsync()
    {
        var manifest = await BuildStagedManifestAsync().ConfigureAwait(true);
        var pf = new PreflightViewModel(manifest);
        if (PreflightDialogHost is not null)
        {
            var confirmed = await PreflightDialogHost(pf).ConfigureAwait(true);
            StatusMessage = confirmed
                ? $"Preflight confirmed: {manifest.Operations.Count} op(s) ready to apply."
                : "Preflight cancelled.";
        }
        else
        {
            StatusMessage = $"Preflight: {manifest.Operations.Count} op(s)" +
                            (manifest.RequiresTypedConfirmation ? " — typed confirmation required." : ".");
        }
    }

    [RelayCommand]
    private async Task OpenTemplateManager()
    {
        // No ConfigureAwait(false): the launcher creates Avalonia windows and must
        // resume on the UI thread.
        if (TemplateManagerLauncher is not null)
            await TemplateManagerLauncher();
    }

    [RelayCommand]
    private void ShowWalkthrough() => WalkthroughLauncher?.Invoke();

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (StagingQueue.Count == 0) { StatusMessage = "Nothing staged."; return; }
        IsBusy = true;
        try
        {
            var manifest = await BuildStagedManifestAsync().ConfigureAwait(true);

            // Enforce typed-confirmation gate for high-risk batches before any registry write.
            if (manifest.RequiresTypedConfirmation)
            {
                if (PreflightDialogHost is null)
                {
                    StatusMessage = "High-risk batch requires Preflight confirmation. Click Preflight first.";
                    return;
                }
                var pf = new PreflightViewModel(manifest);
                var confirmed = await PreflightDialogHost(pf).ConfigureAwait(true);
                if (!confirmed || !pf.TypedConfirmationValid)
                {
                    StatusMessage = "Apply blocked: typed confirmation phrase missing or incorrect.";
                    return;
                }
            }

            var result = await _apply.ApplyAsync(manifest).ConfigureAwait(true);
            StatusMessage = result.Success
                ? $"Applied {result.OperationsSucceeded}/{result.OperationsAttempted}. Snapshot: {Path.GetFileName(result.SnapshotPath)}"
                : $"Apply failed{(result.RolledBack ? " — rolled back" : "")}: {result.Message}";
            if (result.Success)
            {
                StagingQueue.Clear();
                PendingCount = 0;
                await RefreshAsync().ConfigureAwait(true);
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task UndoLastAsync()
    {
        var r = await _apply.UndoLastAsync().ConfigureAwait(true);
        StatusMessage = r.Message ?? (r.Success ? "Undid last apply." : "Undo failed.");
        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task UndoAllAsync()
    {
        var r = await _apply.UndoAllAsync().ConfigureAwait(true);
        StatusMessage = r.Message ?? (r.Success ? "Undid all." : "Undo failed.");
        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private void FlushShellCache()
    {
        _registry.NotifyShellOfChange();
        StatusMessage = "Sent SHCNE_ASSOCCHANGED to Explorer.";
    }

    [RelayCommand]
    private async Task ExportPackAsync()
    {
        // UI shell scaffold: dialog wiring is the host-window's job. Default destination keeps the command testable.
        var dest = Path.Combine(AppPaths.Local, $"cm-editar-pack-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
        var ids = (await _templates.ListAsync().ConfigureAwait(true)).Select(t => t.Id);
        var r = await _templates.ExportPackAsync(ids, dest).ConfigureAwait(true);
        StatusMessage = r.Message ?? (r.Success ? $"Exported pack to {dest}" : "Export failed.");
    }

    [RelayCommand]
    private async Task ImportPackAsync()
    {
        // UI shell scaffold: file picker is wired by the view; this default tries the most-recent pack file.
        var packs = Directory.Exists(AppPaths.Local)
            ? Directory.EnumerateFiles(AppPaths.Local, "cm-editar-pack-*.zip").OrderByDescending(File.GetLastWriteTimeUtc).ToArray()
            : Array.Empty<string>();
        if (packs.Length == 0) { StatusMessage = "No pack file found to import."; return; }
        var r = await _templates.ImportPackAsync(packs[0]).ConfigureAwait(true);
        StatusMessage = r.Message ?? (r.Success ? $"Imported {r.Value?.Count ?? 0} template(s)." : "Import failed.");
    }

    private async Task<ApplyManifest> BuildStagedManifestAsync()
    {
        var staged = StagingQueue.Select(vm => new StagedChange(
            Extension: vm.Entry.Extension,
            Action: vm.StagedAction,
            TargetType: vm.Entry.ShellNewType,
            FileName: vm.Entry.FileName,
            Command: vm.Entry.Command,
            DisplayName: vm.Entry.DisplayName,
            IsNewPlus: vm.Entry.IsNewPlus,
            // Propagate inline payload bytes so Data-type entries reach BuildManifestAsync intact.
            DataBase64: vm.Entry.DataBase64));
        return await _registry.BuildManifestAsync(staged).ConfigureAwait(true);
    }
}
