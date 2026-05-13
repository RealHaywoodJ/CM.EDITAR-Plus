using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CM.EDITAR.Core;
using CM.EDITAR.Registry;

namespace CM.EDITAR.ApplyService;

/// <summary>
/// Default <see cref="IApplyService"/>. Implements the snapshot → apply → SHChangeNotify → verify → rollback pipeline
/// described in the MVP spec. Always funnels every registry write through <see cref="RegistryService.ApplyOperation"/>.
/// </summary>
public sealed class ApplyService : IApplyService
{
    private readonly IRegistryService _registry;
    private readonly ISnapshotService _snapshots;
    private readonly IAuditLog _audit;

    public ApplyService(IRegistryService registry, ISnapshotService snapshots, IAuditLog audit)
    {
        _registry = registry;
        _snapshots = snapshots;
        _audit = audit;
    }

    public async Task<ApplyResult> ApplyAsync(ApplyManifest manifest, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var affectedKeys = manifest.Operations.Select(o => o.KeyPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // 1. Snapshot
        SnapshotMetadata snapshot;
        try
        {
            snapshot = await _snapshots.CreateAsync(affectedKeys, "pre-apply", manifest.Id, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await AppendAuditAsync(manifest, snapshotPath: null, success: false,
                message: $"Snapshot failed: {ex.Message}", affectedKeys, ct).ConfigureAwait(false);
            return new ApplyResult(false, manifest.Id, null, manifest.Operations.Count, 0, false, ex.Message);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Cross-compile branch: refuse to attempt registry writes off-Windows.
            await AppendAuditAsync(manifest, snapshot.RegFilePath, false,
                "Apply skipped: non-Windows host", affectedKeys, ct).ConfigureAwait(false);
            return new ApplyResult(false, manifest.Id, snapshot.RegFilePath, manifest.Operations.Count, 0, false,
                "Registry writes require Windows");
        }

        // 2. Apply
        int applied = 0;
        Exception? failure = null;
        try
        {
            ApplyAllWindows(manifest, ref applied);
        }
        catch (Exception ex) { failure = ex; }

        // 3. Notify shell
        _registry.NotifyShellOfChange();

        // 4. Verify
        var verified = failure is null && await _registry.VerifyManifestAsync(manifest, ct).ConfigureAwait(false);

        if (!verified)
        {
            // 5. Rollback
            var restore = await _snapshots.RestoreAsync(snapshot, ct).ConfigureAwait(false);
            _registry.NotifyShellOfChange();
            await AppendAuditAsync(manifest, snapshot.RegFilePath, false,
                $"Verify failed; rollback {(restore.Success ? "OK" : "FAILED: " + restore.Message)}",
                affectedKeys, ct).ConfigureAwait(false);
            return new ApplyResult(false, manifest.Id, snapshot.RegFilePath, manifest.Operations.Count, applied, true,
                failure?.Message ?? "Verification failed");
        }

        await AppendAuditAsync(manifest, snapshot.RegFilePath, true, "Applied OK", affectedKeys, ct).ConfigureAwait(false);
        return new ApplyResult(true, manifest.Id, snapshot.RegFilePath, manifest.Operations.Count, applied, false, null);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyAllWindows(ApplyManifest manifest, ref int applied)
    {
        foreach (var op in manifest.Operations)
        {
            RegistryService.ApplyOperation(op);
            applied++;
        }
    }

    public async Task<OperationResult> UndoLastAsync(CancellationToken ct = default)
    {
        var all = await _snapshots.ListAsync(ct).ConfigureAwait(false);
        if (all.Count == 0) return OperationResult.Fail("No snapshots to restore.");
        var newest = all[0];
        var result = await _snapshots.RestoreAsync(newest, ct).ConfigureAwait(false);
        _registry.NotifyShellOfChange();
        await AppendUndoAuditAsync(newest, result, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<OperationResult> UndoAllAsync(CancellationToken ct = default)
    {
        var all = await _snapshots.ListAsync(ct).ConfigureAwait(false);
        foreach (var snap in all)
        {
            var r = await _snapshots.RestoreAsync(snap, ct).ConfigureAwait(false);
            await AppendUndoAuditAsync(snap, r, ct).ConfigureAwait(false);
            if (!r.Success) return r;
        }
        _registry.NotifyShellOfChange();
        return OperationResult.Ok($"Restored {all.Count} snapshot(s).");
    }

    private Task AppendAuditAsync(ApplyManifest manifest, string? snapshotPath, bool success, string message, IReadOnlyList<string> keys, CancellationToken ct) =>
        _audit.AppendAsync(new AuditEntry
        {
            UserSid = RegistryService.GetCurrentUserSid(),
            Operation = "apply",
            ManifestId = manifest.Id,
            SnapshotPath = snapshotPath,
            AffectedKeys = keys,
            Success = success,
            Message = message,
        }, ct);

    private Task AppendUndoAuditAsync(SnapshotMetadata snap, OperationResult r, CancellationToken ct) =>
        _audit.AppendAsync(new AuditEntry
        {
            UserSid = RegistryService.GetCurrentUserSid(),
            Operation = "undo",
            SnapshotPath = snap.RegFilePath,
            ManifestId = snap.ManifestId,
            AffectedKeys = snap.ExportedKeys,
            Success = r.Success,
            Message = r.Message,
        }, ct);
}
