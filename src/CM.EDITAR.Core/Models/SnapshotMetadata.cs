using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>
/// Sidecar metadata written next to every <c>*.reg</c> snapshot in
/// <c>%LocalAppData%\CM.EDITAR\Backups\</c>. Used by undo / verify / audit.
/// </summary>
public sealed record SnapshotMetadata
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("userSid")]
    public required string UserSid { get; init; }

    [JsonPropertyName("regFilePath")]
    public required string RegFilePath { get; init; }

    /// <summary>SHA256 of the .reg file bytes, hex-encoded lowercase.</summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("exportedKeys")]
    public IReadOnlyList<string> ExportedKeys { get; init; } = Array.Empty<string>();

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "pre-apply";

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = "1.3.0";

    [JsonPropertyName("manifestId")]
    public Guid? ManifestId { get; init; }
}
