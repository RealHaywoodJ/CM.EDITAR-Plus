using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.Registry;

/// <summary>
/// Default <see cref="ISnapshotService"/>. Snapshots are .reg files plus sidecar JSON metadata
/// stored under <c>%LocalAppData%\CM.EDITAR+\Backups\</c>. Files are never overwritten.
/// </summary>
public sealed class SnapshotService : ISnapshotService
{
    public string BackupsDirectory { get; }

    public SnapshotService(string? backupsDirectory = null)
    {
        BackupsDirectory = backupsDirectory ?? AppPaths.BackupsDir;
        Directory.CreateDirectory(BackupsDirectory);
    }

    public async Task<SnapshotMetadata> CreateAsync(IEnumerable<string> keysToExport, string reason, Guid? manifestId, CancellationToken ct = default)
    {
        var keys = keysToExport.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
        var id = Guid.NewGuid();

        var regPath = Path.Combine(BackupsDirectory, $"{stamp}_{id:N}.reg");
        var metaPath = Path.ChangeExtension(regPath, ".json");
        if (File.Exists(regPath)) throw new IOException($"Refusing to overwrite snapshot {regPath}");

        byte[] regBytes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? RegistryService.ExportKeysToReg(keys)
            : System.Text.Encoding.UTF8.GetBytes($"; CM.EDITAR+ stub snapshot — {keys.Length} keys, non-Windows host\n");

        await File.WriteAllBytesAsync(regPath, regBytes, ct).ConfigureAwait(false);

        var sha = Convert.ToHexString(SHA256.HashData(regBytes)).ToLowerInvariant();
        var meta = new SnapshotMetadata
        {
            Id = id,
            UserSid = RegistryService.GetCurrentUserSid(),
            RegFilePath = regPath,
            Sha256 = sha,
            ExportedKeys = keys,
            Reason = reason,
            ManifestId = manifestId,
        };

        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta, JsonOpts), ct).ConfigureAwait(false);
        return meta;
    }

    public async Task<IReadOnlyList<SnapshotMetadata>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(BackupsDirectory)) return Array.Empty<SnapshotMetadata>();
        var results = new List<SnapshotMetadata>();
        foreach (var meta in Directory.EnumerateFiles(BackupsDirectory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(meta, ct).ConfigureAwait(false);
                var parsed = JsonSerializer.Deserialize<SnapshotMetadata>(json, JsonOpts);
                if (parsed is not null) results.Add(parsed);
            }
            catch { /* skip corrupt sidecar */ }
        }
        return results.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public async Task<OperationResult> RestoreAsync(SnapshotMetadata snapshot, CancellationToken ct = default)
    {
        if (!File.Exists(snapshot.RegFilePath))
            return OperationResult.Fail($"Snapshot file missing: {snapshot.RegFilePath}");

        var bytes = await File.ReadAllBytesAsync(snapshot.RegFilePath, ct).ConfigureAwait(false);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(sha, snapshot.Sha256, StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail($"Snapshot SHA256 mismatch — refusing restore. Expected {snapshot.Sha256}, got {sha}.");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OperationResult.Fail("Snapshot restore requires Windows (regedit /s).");

        return RestoreWindows(snapshot.RegFilePath);
    }

    [SupportedOSPlatform("windows")]
    private static OperationResult RestoreWindows(string regPath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("regedit.exe", $"/s \"{regPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            p.WaitForExit(15_000);
            return p.ExitCode == 0
                ? OperationResult.Ok($"Restored {regPath}")
                : OperationResult.Fail($"regedit exited with code {p.ExitCode}");
        }
        catch (Exception ex) { return OperationResult.Fail("Restore failed", ex); }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
}
