using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.ApplyService;

/// <summary>
/// Append-only audit log. Each line is JSON; the <see cref="AuditEntry.Signature"/> field is HMAC-SHA256
/// over the canonical JSON of the entry (with the signature field omitted), keyed by the per-install secret.
/// </summary>
public sealed class AuditLog : IAuditLog
{
    private readonly ISecretStore _secrets;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AuditLog(ISecretStore secrets, string? logPath = null)
    {
        _secrets = secrets;
        LogPath = logPath ?? AppPaths.AuditLogFile;
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    }

    public string LogPath { get; }

    public async Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var token = await _secrets.GetOrCreateTokenAsync(ct).ConfigureAwait(false);
        var unsigned = entry with { Signature = null };
        var canonical = JsonSerializer.Serialize(unsigned, JsonOpts);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token));
        var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        var signed = entry with { Signature = sig };
        var line = JsonSerializer.Serialize(signed, JsonOpts);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try { await File.AppendAllTextAsync(LogPath, line + Environment.NewLine, ct).ConfigureAwait(false); }
        finally { _lock.Release(); }
    }

    public async Task<IReadOnlyList<AuditEntry>> ReadAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(LogPath)) return Array.Empty<AuditEntry>();
        var lines = await File.ReadAllLinesAsync(LogPath, ct).ConfigureAwait(false);
        var results = new List<AuditEntry>(lines.Length);
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            try
            {
                var entry = JsonSerializer.Deserialize<AuditEntry>(raw, JsonOpts);
                if (entry is not null) results.Add(entry);
            }
            catch { /* skip corrupt line */ }
        }
        return results;
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };
}
