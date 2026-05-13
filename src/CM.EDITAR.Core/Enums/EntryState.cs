namespace CM.EDITAR.Core;

/// <summary>Lifecycle state of a ShellNew entry as observed (not staged).</summary>
public enum EntryState
{
    Enabled = 0,
    Disabled = 1,
    Missing = 2,
    Pending = 3,
}

/// <summary>An action staged against an entry, awaiting Apply.</summary>
public enum StagedAction
{
    None = 0,
    Enable = 1,
    Disable = 2,
    Edit = 3,
    Add = 4,
    Remove = 5,
}
