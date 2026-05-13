namespace CM.EDITAR.Core;

/// <summary>
/// Contract for the Windows registry abstraction. Discovery may read HKCU and HKCR;
/// runtime writes MUST be HKCU only. All implementations must funnel writes through
/// <see cref="BuildManifestAsync"/> + <see cref="IApplyService"/>.
/// </summary>
public interface IRegistryService
{
    /// <summary>True when running on a platform that supports real registry I/O.</summary>
    bool IsSupported { get; }

    /// <summary>Discover every ShellNew entry visible to the current user. HKCU first, HKCR fallback.</summary>
    Task<IReadOnlyList<ShellNewEntry>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>Resolve the effective ProgID for an extension, honoring UserChoice if present.</summary>
    Task<ProgIdResolution> ResolveProgIdAsync(string extension, CancellationToken ct = default);

    /// <summary>
    /// Build a dry-run manifest for a set of staged changes. Pure function — performs no writes.
    /// </summary>
    Task<ApplyManifest> BuildManifestAsync(IEnumerable<StagedChange> staged, CancellationToken ct = default);

    /// <summary>Notify Explorer that ShellNew registrations changed (SHChangeNotify SHCNE_ASSOCCHANGED).</summary>
    void NotifyShellOfChange();

    /// <summary>Re-read the registry and confirm the manifest's intent is reflected. Used by verify.</summary>
    Task<bool> VerifyManifestAsync(ApplyManifest manifest, CancellationToken ct = default);
}

/// <summary>One pending change staged in the UI before an Apply.</summary>
public sealed record StagedChange(
    string Extension,
    StagedAction Action,
    ShellNewType TargetType,
    string? FileName = null,
    string? Command = null,
    string? DisplayName = null,
    bool IsNewPlus = false,
    Guid? TemplateId = null,
    string? DataBase64 = null);

/// <summary>Output of <see cref="IRegistryService.ResolveProgIdAsync"/>.</summary>
public sealed record ProgIdResolution(
    string Extension,
    string? ProgId,
    bool FromUserChoice,
    bool FromHkcuOverride,
    string ResolvedShellNewKeyPath);
