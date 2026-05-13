namespace CM.EDITAR.FileCreator;

/// <summary>
/// Atomic file writer. Writes to a temp file in the same directory as the target
/// (so it's on the same volume), then performs an atomic rename via
/// <see cref="File.Move(string, string, bool)"/>. The rename never overwrites unless requested.
/// </summary>
public static class AtomicWriter
{
    public static async Task WriteAsync(string targetPath, byte[] content, bool overwrite, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(targetPath))
                  ?? throw new IOException($"Cannot determine directory for {targetPath}");
        Directory.CreateDirectory(dir);

        var tmp = Path.Combine(dir, $".cmeditar~{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await fs.WriteAsync(content, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
            }
            File.Move(tmp, targetPath, overwrite);
        }
        catch
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best effort */ }
            throw;
        }
    }
}
