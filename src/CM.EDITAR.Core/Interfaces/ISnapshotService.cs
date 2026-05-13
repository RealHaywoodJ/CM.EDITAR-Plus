namespace CM.EDITAR.Core;

/// <summary>Creates and restores .reg snapshots used by the Apply pipeline's undo stack.</summary>
public interface ISnapshotService
{
    /// <summary>
    /// Export the given keys to a timestamped .reg file under the Backups directory and write a sidecar metadata file.
    /// Never overwrites existing snapshots.
    /// </summary>
    Task<SnapshotMetadata> CreateAsync(IEnumerable<string> keysToExport, string reason, Guid? manifestId, CancellationToken ct = default);

    /// <summary>List all known snapshots, newest first.</summary>
    Task<IReadOnlyList<SnapshotMetadata>> ListAsync(CancellationToken ct = default);

    /// <summary>Restore a snapshot by importing its .reg file. Verifies SHA256 first.</summary>
    Task<OperationResult> RestoreAsync(SnapshotMetadata snapshot, CancellationToken ct = default);

    /// <summary>The directory where snapshots are written (typically %LocalAppData%\CM.EDITAR\Backups).</summary>
    string BackupsDirectory { get; }
}
