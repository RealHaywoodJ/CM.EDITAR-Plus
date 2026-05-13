namespace CM.EDITAR.Core;

/// <summary>Append-only signed audit log of every Apply / Undo operation.</summary>
public interface IAuditLog
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> ReadAllAsync(CancellationToken ct = default);
    string LogPath { get; }
}

/// <summary>Helper used by the elevated WiX custom action and the in-app installer flow.</summary>
public interface IInstallerHelper
{
    Task<OperationResult> RegisterNewPlusAsync(CancellationToken ct = default);
    Task<OperationResult> UnregisterNewPlusAsync(CancellationToken ct = default);
    Task<SnapshotMetadata> CreateInstallerSnapshotAsync(CancellationToken ct = default);
    Task<OperationResult> RestoreInstallerSnapshotAsync(CancellationToken ct = default);
}
