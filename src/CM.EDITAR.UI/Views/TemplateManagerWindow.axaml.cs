using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CM.EDITAR.UI.ViewModels;

namespace CM.EDITAR.UI.Views;

public partial class TemplateManagerWindow : Window
{
    public TemplateManagerWindow() => InitializeComponent();

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        DataContextChanged += OnDataContextChanged;
    }

    private TemplateManagerViewModel? _previousVm;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousVm is not null)
        {
            _previousVm.SavePackDialogHost = null;
            _previousVm.OpenPackDialogHost = null;
            _previousVm = null;
        }

        if (DataContext is TemplateManagerViewModel vm)
        {
            vm.SavePackDialogHost = ShowSavePackDialogAsync;
            vm.OpenPackDialogHost = ShowOpenPackDialogAsync;
            _previousVm = vm;
        }
    }

    private async Task<string?> ShowSavePackDialogAsync()
    {
        var sp = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (sp is null) return null;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export template pack",
            SuggestedFileName = $"template-pack-{DateTimeOffset.UtcNow:yyyyMMdd}",
            DefaultExtension = "zip",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Template Pack (*.zip)") { Patterns = new[] { "*.zip" } },
            },
        });

        return file?.Path.LocalPath;
    }

    private async Task<string?> ShowOpenPackDialogAsync()
    {
        var sp = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (sp is null) return null;

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import template pack",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Template Pack (*.zip)") { Patterns = new[] { "*.zip" } },
            },
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
