using CM.EDITAR.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CM.EDITAR.UI.ViewModels;

/// <summary>Per-row VM for the main extensions DataGrid. Tracks staging state independently.</summary>
public partial class ShellNewEntryViewModel : ViewModelBase
{
    public ShellNewEntry Entry { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isStaged;
    [ObservableProperty] private StagedAction _stagedAction = StagedAction.None;
    [ObservableProperty] private int _queuePosition;

    public ShellNewEntryViewModel(ShellNewEntry entry) => Entry = entry;

    public string Extension => Entry.Extension;
    public string DisplayName => Entry.DisplayName;
    public string Group => Entry.Group;
    public EntryState State => Entry.State;
    public RiskLevel Risk => Entry.Risk;
    public string? Description => Entry.Description ?? Entry.SourceKey;
    public string RiskBadge => Risk switch
    {
        RiskLevel.Recommended => "REC",
        RiskLevel.Warning => "WARN",
        RiskLevel.High => "HIGH",
        _ => "—",
    };
}
