using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>JSON request sent over the named pipe to <c>CM.EDITAR.FileCreator --serve</c>.</summary>
public sealed record FileCreatorRequest
{
    [JsonPropertyName("templateId")] public required Guid TemplateId { get; init; }
    [JsonPropertyName("targetPath")] public required string TargetPath { get; init; }
    [JsonPropertyName("token")] public required string Token { get; init; }
    [JsonPropertyName("placeholderOverrides")]
    public IReadOnlyDictionary<string, string>? PlaceholderOverrides { get; init; }
}

/// <summary>JSON response returned by the FileCreator named pipe server.</summary>
public sealed record FileCreatorResponse
{
    [JsonPropertyName("success")] public required bool Success { get; init; }
    [JsonPropertyName("createdPath")] public string? CreatedPath { get; init; }
    [JsonPropertyName("diagnostics")] public string? Diagnostics { get; init; }
    [JsonPropertyName("errorCode")] public string? ErrorCode { get; init; }
}
