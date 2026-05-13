using System.Security.Cryptography;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.Templates;

/// <summary>
/// Seeds and selectively upgrades built-in starter-pack templates in the user's Templates directory.
/// <para>
/// On every run each pack template is evaluated individually:
/// <list type="bullet">
///   <item>Not yet installed → copied in.</item>
///   <item>Installed, same or newer <c>builtInVersion</c> → left alone.</item>
///   <item>Installed, older <c>builtInVersion</c>, user has not drifted → overwritten.</item>
///   <item>Installed, older <c>builtInVersion</c>, user has drifted → left alone.</item>
/// </list>
/// </para>
/// <para>
/// "User has drifted" is detected by two independent checks, either of which is
/// sufficient to preserve the installed copy:
/// <list type="number">
///   <item>
///     <b>Body hash check</b> — at import time the SHA-256 of the body file is stored
///     in <c>metadata.json</c> as <c>builtInBodyHash</c>.  On upgrade the hash of the
///     currently installed body is compared against that baseline.  A mismatch means the
///     user (or some other process) edited the body file directly on disk.
///   </item>
///   <item>
///     <b>Metadata timestamp check</b> — <c>UpdateAsync</c> bumps <c>modifiedAt</c>; if
///     <c>modifiedAt</c> is meaningfully later than <c>createdAt</c> the user changed
///     the template's metadata fields through the UI.
///   </item>
/// </list>
/// Legacy installs that pre-date the hash feature fall back to timestamp-only detection.
/// </para>
/// </summary>
public static class StarterPackImporter
{
    private const string PackSubPath = "templates/starter-pack";

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs the selective import/upgrade for every template in the starter pack.
    /// </summary>
    /// <param name="templatesDir">Destination — the user's Templates folder.</param>
    /// <param name="packBaseDir">
    /// Optional override for the source pack root. Useful in tests.
    /// When <see langword="null"/>, resolved from <see cref="AppContext.BaseDirectory"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task RunAsync(
        string? templatesDir = null,
        string? packBaseDir = null,
        CancellationToken ct = default)
    {
        var destination = templatesDir ?? AppPaths.TemplatesDir;
        Directory.CreateDirectory(destination);

        var packDir = packBaseDir ?? Path.Combine(AppContext.BaseDirectory, PackSubPath);
        if (!Directory.Exists(packDir))
            return;

        foreach (var templateSrcDir in Directory.EnumerateDirectories(packDir))
        {
            ct.ThrowIfCancellationRequested();

            var dirName = Path.GetFileName(templateSrcDir);
            if (!Guid.TryParse(dirName, out _))
                continue;

            var destDir = Path.Combine(destination, dirName);
            var destMeta = Path.Combine(destDir, "metadata.json");

            if (!Directory.Exists(destDir) || !File.Exists(destMeta))
            {
                // Not installed yet — seed it.
                await CopyTemplateAsync(templateSrcDir, destDir, ct).ConfigureAwait(false);
                continue;
            }

            var packVersion = await ReadBuiltInVersionAsync(
                Path.Combine(templateSrcDir, "metadata.json"), ct).ConfigureAwait(false);

            var installedMeta = await ReadMetadataAsync(destMeta, ct).ConfigureAwait(false);
            var installedVersion = installedMeta?.BuiltInVersion;

            if (!IsNewerVersion(packVersion, installedVersion))
                continue;

            if (installedMeta is not null &&
                await HasUserDriftAsync(destDir, installedMeta, ct).ConfigureAwait(false))
                continue;

            await CopyTemplateAsync(templateSrcDir, destDir, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Unconditionally restores a single built-in template from the shipped starter pack,
    /// overwriting whatever the user may have changed. Useful for a "Restore defaults" action.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the template was found in the pack and restored;
    /// <see langword="false"/> when the ID is not present in the pack.
    /// </returns>
    public static async Task<bool> RestoreTemplateAsync(
        Guid id,
        string? templatesDir = null,
        string? packBaseDir = null,
        CancellationToken ct = default)
    {
        var destination = templatesDir ?? AppPaths.TemplatesDir;
        var packDir = packBaseDir ?? Path.Combine(AppContext.BaseDirectory, PackSubPath);
        var templateSrcDir = Path.Combine(packDir, id.ToString());

        if (!Directory.Exists(templateSrcDir))
            return false;

        var destDir = Path.Combine(destination, id.ToString());
        await CopyTemplateAsync(templateSrcDir, destDir, ct).ConfigureAwait(false);
        return true;
    }

    // -------------------------------------------------------------------------
    // Drift detection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns <see langword="true"/> when the installed template has drifted from the
    /// baseline recorded at import time, indicating the user has made changes that must
    /// be preserved.
    /// <para>
    /// Two independent signals are checked; either is sufficient to report drift:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     Body hash: SHA-256 of the current body file vs <c>builtInBodyHash</c> stored
    ///     in <c>metadata.json</c>.  Catches direct on-disk edits that bypass the UI.
    ///   </item>
    ///   <item>
    ///     Metadata timestamp: <c>modifiedAt &gt; createdAt + 1 s</c>.  Catches edits
    ///     made through <c>UpdateAsync</c> (name, extensions, placeholders, etc.).
    ///   </item>
    /// </list>
    /// Legacy installs without a stored hash fall back to the timestamp check only.
    /// </summary>
    public static async Task<bool> HasUserDriftAsync(
        string installedDir,
        TemplateMetadata installedMeta,
        CancellationToken ct = default)
    {
        // --- Body drift: compare current body bytes against the hash stored at import ---
        if (installedMeta.BuiltInBodyHash is string expectedHash)
        {
            var currentHash = await ComputeBodyHashAsync(installedDir, ct).ConfigureAwait(false);
            if (!string.Equals(currentHash ?? string.Empty, expectedHash, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // --- Metadata drift: UpdateAsync bumps modifiedAt; detect UI-driven edits ---
        if (installedMeta.ModifiedAt > installedMeta.CreatedAt.AddSeconds(1))
            return true;

        return false;
    }

    // -------------------------------------------------------------------------
    // Version comparison
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="packVersion"/> is strictly
    /// newer than <paramref name="installedVersion"/>.
    /// A non-null pack version always beats a null installed version.
    /// </summary>
    public static bool IsNewerVersion(string? packVersion, string? installedVersion)
    {
        if (packVersion is null)
            return false;

        if (installedVersion is null)
            return true;

        if (Version.TryParse(NormalizeVersion(packVersion), out var pv) &&
            Version.TryParse(NormalizeVersion(installedVersion), out var iv))
            return pv > iv;

        return string.Compare(packVersion, installedVersion, StringComparison.OrdinalIgnoreCase) > 0;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Copies all files from <paramref name="srcDir"/> to <paramref name="destDir"/>
    /// (overwriting), then rewrites <c>metadata.json</c> to stamp the install
    /// timestamps and record a SHA-256 digest of the body file.
    /// </summary>
    private static async Task CopyTemplateAsync(string srcDir, string destDir, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.EnumerateFiles(srcDir))
        {
            ct.ThrowIfCancellationRequested();
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        await StampMetadataAsync(destDir, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Rewrites <c>createdAt</c>/<c>modifiedAt</c> to the current time and updates
    /// (or inserts) <c>builtInBodyHash</c> with the SHA-256 of the body file.
    /// </summary>
    private static async Task StampMetadataAsync(string destDir, CancellationToken ct)
    {
        var metaPath = Path.Combine(destDir, "metadata.json");
        if (!File.Exists(metaPath))
            return;

        try
        {
            var bodyHash = await ComputeBodyHashAsync(destDir, ct).ConfigureAwait(false);

            var json = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var now = DateTimeOffset.UtcNow.ToString("o");
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                bool hashWritten = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name is "createdAt" or "modifiedAt")
                        writer.WriteString(prop.Name, now);
                    else if (prop.Name == "builtInBodyHash")
                    {
                        writer.WriteString(prop.Name, bodyHash ?? string.Empty);
                        hashWritten = true;
                    }
                    else
                        prop.WriteTo(writer);
                }
                // Insert the hash field if the source metadata.json didn't have it yet.
                if (!hashWritten)
                    writer.WriteString("builtInBodyHash", bodyHash ?? string.Empty);
                writer.WriteEndObject();
            }

            await File.WriteAllBytesAsync(metaPath, ms.ToArray(), ct).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal: a stale timestamp or missing hash is acceptable.
        }
    }

    /// <summary>
    /// Returns the SHA-256 hex digest of the first non-metadata body file found
    /// in <paramref name="dir"/>, or <see langword="null"/> if no body file exists.
    /// </summary>
    private static async Task<string?> ComputeBodyHashAsync(string dir, CancellationToken ct)
    {
        var bodyFile = Directory.EnumerateFiles(dir)
            .FirstOrDefault(f => !Path.GetFileName(f).Equals("metadata.json",
                StringComparison.OrdinalIgnoreCase));
        if (bodyFile is null)
            return null;

        var bytes = await File.ReadAllBytesAsync(bodyFile, ct).ConfigureAwait(false);
        if (bytes.Length == 0)
            return null;

        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static async Task<string?> ReadBuiltInVersionAsync(string metaPath, CancellationToken ct)
    {
        if (!File.Exists(metaPath))
            return null;
        try
        {
            var json = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("builtInVersion", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        catch { }
        return null;
    }

    private static async Task<TemplateMetadata?> ReadMetadataAsync(string metaPath, CancellationToken ct)
    {
        if (!File.Exists(metaPath))
            return null;
        try
        {
            var json = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TemplateMetadata>(json);
        }
        catch { return null; }
    }

    /// <summary>
    /// Strips pre-release/build-metadata suffixes so the string can be parsed by
    /// <see cref="Version"/>.  E.g. "1.2.3-rc.1" → "1.2.3".
    /// </summary>
    private static string NormalizeVersion(string v)
    {
        var dash = v.IndexOf('-');
        return dash >= 0 ? v[..dash] : v;
    }
}
