using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>Per-placeholder definition for a template.</summary>
public sealed record PlaceholderSpec(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("default")] string? Default,
    [property: JsonPropertyName("description")] string? Description);

/// <summary>
/// Metadata for a template managed by <see cref="ITemplateService"/>.
/// Persisted as <c>%AppData%\CM.EDITAR\Templates\&lt;id&gt;\metadata.json</c>.
/// </summary>
public sealed record TemplateMetadata
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("extensions")]
    public IReadOnlyList<string> Extensions { get; init; } = Array.Empty<string>();

    [JsonPropertyName("category")]
    public string Category { get; init; } = "Custom";

    [JsonPropertyName("defaultFilename")]
    public string DefaultFilename { get; init; } = "New File";

    [JsonPropertyName("templateType")]
    public ShellNewType TemplateType { get; init; } = ShellNewType.FileName;

    /// <summary>Path on disk (FileName) or command line (Command). Null for NullFile / Data.</summary>
    [JsonPropertyName("templateSource")]
    public string? TemplateSource { get; init; }

    /// <summary>Inline bytes for Data templates, Base64 encoded.</summary>
    [JsonPropertyName("dataBase64")]
    public string? DataBase64 { get; init; }

    [JsonPropertyName("placeholders")]
    public IReadOnlyList<PlaceholderSpec> Placeholders { get; init; } = Array.Empty<PlaceholderSpec>();

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("modifiedAt")]
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("author")]
    public string Author { get; init; } = Environment.UserName;

    /// <summary>True when the user has explicitly approved this Command template (sanitization gate).</summary>
    [JsonPropertyName("commandApproved")]
    public bool CommandApproved { get; init; }

    /// <summary>
    /// Semver string set by the shipped starter pack (e.g. "1.0.0").
    /// <see langword="null"/> for templates the user created themselves.
    /// Used by <c>StarterPackImporter</c> to determine whether a built-in
    /// template should be refreshed on upgrade.
    /// </summary>
    [JsonPropertyName("builtInVersion")]
    public string? BuiltInVersion { get; init; }

    /// <summary>
    /// SHA-256 hex digest of the template body bytes at the time the starter pack was imported.
    /// <see langword="null"/> for user-created templates or legacy installs without a stored hash.
    /// <para>
    /// <c>StarterPackImporter</c> records this when it seeds or restores a built-in template.
    /// On subsequent upgrade runs it compares the digest of the body file on disk against this
    /// value to determine whether the user has edited the body outside of the UI.
    /// </para>
    /// </summary>
    [JsonPropertyName("builtInBodyHash")]
    public string? BuiltInBodyHash { get; init; }
}
