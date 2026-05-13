using System.Text.Json.Serialization;

namespace CM.EDITAR.Core;

/// <summary>The kind of registry change a single operation performs.</summary>
public enum RegistryOperationKind { Add, Modify, Delete, DeleteValue }

/// <summary>One concrete registry write the apply pipeline will perform.</summary>
public sealed record RegistryOperation
{
    [JsonPropertyName("kind")] public required RegistryOperationKind Kind { get; init; }

    /// <summary>Always rooted in HKCU at apply time. Read for HKCR is fine.</summary>
    [JsonPropertyName("keyPath")] public required string KeyPath { get; init; }

    /// <summary>Empty string for the (Default) value.</summary>
    [JsonPropertyName("valueName")] public string ValueName { get; init; } = "";

    /// <summary>REG_SZ / REG_BINARY / REG_NONE / REG_EXPAND_SZ / REG_DWORD.</summary>
    [JsonPropertyName("valueKind")] public string ValueKind { get; init; } = "REG_SZ";

    /// <summary>String form of the value being written. For REG_BINARY, hex pairs.</summary>
    [JsonPropertyName("value")] public string? Value { get; init; }

    [JsonPropertyName("comment")] public string? Comment { get; init; }
}

/// <summary>
/// Result of preflight: a fully resolved set of operations to be applied as one atomic batch.
/// Emitted by <see cref="IRegistryService.BuildManifestAsync"/>; consumed by <see cref="IApplyService"/>.
/// </summary>
public sealed record ApplyManifest
{
    [JsonPropertyName("id")] public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("operations")] public IReadOnlyList<RegistryOperation> Operations { get; init; } = Array.Empty<RegistryOperation>();

    /// <summary>Snapshot/Apply require user-typed confirmation when this is true.</summary>
    [JsonPropertyName("requiresTypedConfirmation")] public bool RequiresTypedConfirmation { get; init; }

    [JsonPropertyName("requiresElevation")] public bool RequiresElevation { get; init; }

    [JsonPropertyName("notes")] public string? Notes { get; init; }
}
