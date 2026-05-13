using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>
/// Canonical, JSON-serializable description of a single ShellNew entry as discovered or staged.
/// Mirrors the discovery output schema in the MVP specification.
/// </summary>
public sealed record ShellNewEntry
{
    /// <summary>The file extension including the leading dot (e.g. ".md"). Use ".(blank)" for the extensionless entry.</summary>
    [JsonPropertyName("extension")]
    public required string Extension { get; init; }

    /// <summary>Display name as it should appear in the Explorer "New" submenu (ItemName).</summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>Which of the four ShellNew variants this entry uses.</summary>
    [JsonPropertyName("shellNewType")]
    public ShellNewType ShellNewType { get; init; }

    /// <summary>Path to the static template file (FileName variant).</summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }

    /// <summary>Command line executed to produce the file (Command variant).</summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>Inline binary data (Data variant), Base64 encoded for JSON transport.</summary>
    [JsonPropertyName("data")]
    public string? DataBase64 { get; init; }

    /// <summary>Whether the entry is currently visible in the New submenu.</summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; init; } = true;

    /// <summary>The exact registry path the entry was discovered at (e.g. "HKCU\\Software\\Classes\\.md\\ShellNew").</summary>
    [JsonPropertyName("sourceKey")]
    public required string SourceKey { get; init; }

    /// <summary>Hive the entry was found in (HKCU or HKCR).</summary>
    [JsonPropertyName("sourceHive")]
    public RegistryHive SourceHive { get; init; } = RegistryHive.HKCU;

    /// <summary>Resolved ProgID, if any (e.g. "txtfile" for .txt). Null when no ProgID maps the extension.</summary>
    [JsonPropertyName("progIdResolved")]
    public string? ProgIdResolved { get; init; }

    /// <summary>True when the extension's UserChoice key redirects ProgID — must be honored, never overwritten.</summary>
    [JsonPropertyName("hasUserChoice")]
    public bool HasUserChoice { get; init; }

    /// <summary>Risk classification computed by the discovery service.</summary>
    [JsonPropertyName("risk")]
    public RiskLevel Risk { get; init; } = RiskLevel.Recommended;

    /// <summary>Observed lifecycle state.</summary>
    [JsonPropertyName("state")]
    public EntryState State { get; init; } = EntryState.Enabled;

    /// <summary>Logical group/category (Text/Data, Office/Docs, Power User, etc.).</summary>
    [JsonPropertyName("group")]
    public string Group { get; init; } = "Text/Data";

    /// <summary>Pack identifier this entry belongs to (e.g. "cm-editar.core", "system", "user").</summary>
    [JsonPropertyName("pack")]
    public string Pack { get; init; } = "system";

    /// <summary>Optional human-readable description shown in the inspector.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>True when this entry belongs to the per-user "New+" submenu (vs. the default New).</summary>
    [JsonPropertyName("isNewPlus")]
    public bool IsNewPlus { get; init; }
}
