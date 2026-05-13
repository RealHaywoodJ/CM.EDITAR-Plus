using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>
/// One signed line in <c>%LocalAppData%\CM.EDITAR\Audit\changes.log</c>.
/// Lines are JSON-serialized; the <see cref="Signature"/> is computed over the canonical JSON
/// without the signature itself (HMAC-SHA256 with the per-install secret).
/// </summary>
public sealed record AuditEntry
{
    [JsonPropertyName("ts")] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    [JsonPropertyName("userSid")] public required string UserSid { get; init; }
    [JsonPropertyName("operation")] public required string Operation { get; init; }
    [JsonPropertyName("snapshotPath")] public string? SnapshotPath { get; init; }
    [JsonPropertyName("manifestId")] public Guid? ManifestId { get; init; }
    [JsonPropertyName("affectedKeys")] public IReadOnlyList<string> AffectedKeys { get; init; } = Array.Empty<string>();
    [JsonPropertyName("success")] public bool Success { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("appVersion")] public string AppVersion { get; init; } = "1.3.0";
    [JsonPropertyName("sig")] public string? Signature { get; init; }
}
