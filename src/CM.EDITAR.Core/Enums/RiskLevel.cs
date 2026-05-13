namespace CM.EDITAR.Core;

/// <summary>Risk classification for a discovered or staged ShellNew entry.</summary>
public enum RiskLevel
{
    /// <summary>Recommended: well-known data extension with safe default behavior.</summary>
    Recommended = 0,
    /// <summary>Warning: may require external handler or affects power-user workflows.</summary>
    Warning = 1,
    /// <summary>High risk: executes code on creation or affects system stability.</summary>
    High = 2,
}
