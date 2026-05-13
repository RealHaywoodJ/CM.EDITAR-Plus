using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using CM.EDITAR.Core;
using CM.EDITAR.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CM.EDITAR.UI.ViewModels;

public partial class TemplateManagerViewModel : ViewModelBase
{
    private readonly ITemplateService _templates;

    public ObservableCollection<TemplateMetadata> Templates { get; } = new();

    /// <summary>Names of templates that were quarantined (sanitized) on the most recent import.</summary>
    public ObservableCollection<string> QuarantinedTemplateNames { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreDefaultCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPackCommand))]
    private TemplateMetadata? _selected;

    [ObservableProperty] private string _previewText = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _hasQuarantinedTemplates;

    /// <summary>Hook set by the View to show a SaveFileDialog and return the chosen path, or null if cancelled.</summary>
    public Func<Task<string?>>? SavePackDialogHost { get; set; }

    /// <summary>Hook set by the View to show an OpenFileDialog and return the chosen path, or null if cancelled.</summary>
    public Func<Task<string?>>? OpenPackDialogHost { get; set; }

    public TemplateManagerViewModel(ITemplateService templates)
    {
        _templates = templates;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Templates.Clear();
        foreach (var t in await _templates.ListAsync().ConfigureAwait(true)) Templates.Add(t);
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (Selected is null) return;
        try
        {
            var bytes = await _templates.RenderAsync(Selected.Id, overrides: null).ConfigureAwait(true);
            PreviewText = System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) { PreviewText = $"<preview failed: {ex.Message}>"; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Selected is null) return;
        await _templates.DeleteAsync(Selected.Id).ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Re-seeds the selected built-in template from the shipped starter pack,
    /// discarding any edits the user may have made.
    /// Only available when the selected template has a <c>builtInVersion</c>.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestoreDefault))]
    private async Task RestoreDefaultAsync()
    {
        if (Selected is null) return;
        var packDir = Path.Combine(AppContext.BaseDirectory, "templates/starter-pack");
        await StarterPackImporter
            .RestoreTemplateAsync(Selected.Id, _templates.TemplatesDirectory, packDir)
            .ConfigureAwait(true);
        await RefreshAsync().ConfigureAwait(true);
    }

    private bool CanRestoreDefault() => Selected?.BuiltInVersion is not null;

    /// <summary>
    /// Exports the currently selected template as a zip pack.
    /// Opens a SaveFileDialog via <see cref="SavePackDialogHost"/> to let the user choose the destination.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExportPack))]
    private async Task ExportPackAsync()
    {
        if (Selected is null) return;

        string? path = null;
        if (SavePackDialogHost is not null)
            path = await SavePackDialogHost().ConfigureAwait(true);

        if (path is null)
        {
            StatusMessage = "Export cancelled.";
            return;
        }

        var r = await _templates.ExportPackAsync([Selected.Id], path).ConfigureAwait(true);
        StatusMessage = r.Message ?? (r.Success ? $"Exported pack to {Path.GetFileName(path)}." : "Export failed.");
    }

    private bool CanExportPack() => Selected is not null;

    /// <summary>
    /// Imports a zip pack chosen via an OpenFileDialog.
    /// After import, refreshes the template list and shows a quarantine banner
    /// for any templates that were sanitized during import.
    /// </summary>
    [RelayCommand]
    private async Task ImportPackAsync()
    {
        string? path = null;
        if (OpenPackDialogHost is not null)
            path = await OpenPackDialogHost().ConfigureAwait(true);

        if (path is null)
        {
            StatusMessage = "Import cancelled.";
            return;
        }

        // Snapshot which templates in the pack are Command type before extraction,
        // so we can identify which ones were quarantined by the sanitizer.
        var commandIds = SniffCommandIds(path);

        var r = await _templates.ImportPackAsync(path).ConfigureAwait(true);
        if (!r.Success)
        {
            StatusMessage = r.Message ?? "Import failed.";
            return;
        }

        var imported = r.Value ?? (IReadOnlyList<TemplateMetadata>)Array.Empty<TemplateMetadata>();

        // Quarantined = was a Command template in the zip but landed as NullFile (sanitized).
        QuarantinedTemplateNames.Clear();
        foreach (var t in imported)
        {
            if (commandIds.Contains(t.Id) &&
                t.TemplateType == ShellNewType.NullFile &&
                !t.CommandApproved)
            {
                QuarantinedTemplateNames.Add(t.Name);
            }
        }
        HasQuarantinedTemplates = QuarantinedTemplateNames.Count > 0;

        StatusMessage = $"Imported {imported.Count} template(s)." +
                        (HasQuarantinedTemplates ? " Some templates were quarantined." : "");

        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private void DismissQuarantine()
    {
        QuarantinedTemplateNames.Clear();
        HasQuarantinedTemplates = false;
    }

    /// <summary>
    /// Reads the zip at <paramref name="zipPath"/> without extracting and returns the IDs of
    /// any entries whose <c>metadata.json</c> declares <c>TemplateType == Command</c>.
    /// Used to detect which templates were sanitized during <see cref="ImportPackAsync"/>.
    /// </summary>
    private static HashSet<Guid> SniffCommandIds(string zipPath)
    {
        var result = new HashSet<Guid>();
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (!entry.Name.Equals("metadata.json", StringComparison.OrdinalIgnoreCase)) continue;
                using var stream = entry.Open();
                var meta = JsonSerializer.Deserialize<TemplateMetadata>(stream);
                if (meta is { TemplateType: ShellNewType.Command })
                    result.Add(meta.Id);
            }
        }
        catch { /* best-effort; non-fatal */ }
        return result;
    }
}
