using System.Collections.ObjectModel;
using CM.EDITAR.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CM.EDITAR.UI.ViewModels;

/// <summary>Backing VM for the Preflight (dry-run) dialog. Read-only — never writes the registry.</summary>
public partial class PreflightViewModel : ViewModelBase
{
    public ApplyManifest Manifest { get; }
    public ObservableCollection<RegistryOperation> Operations { get; }

    [ObservableProperty] private string _typedConfirmation = "";
    [ObservableProperty] private bool _userAccepted;

    public PreflightViewModel(ApplyManifest manifest)
    {
        Manifest = manifest;
        Operations = new ObservableCollection<RegistryOperation>(manifest.Operations);
    }

    public string ConfirmationPhrase => "I UNDERSTAND THE RISK";
    public bool TypedConfirmationValid =>
        !Manifest.RequiresTypedConfirmation ||
        string.Equals(TypedConfirmation, ConfirmationPhrase, StringComparison.Ordinal);
}
