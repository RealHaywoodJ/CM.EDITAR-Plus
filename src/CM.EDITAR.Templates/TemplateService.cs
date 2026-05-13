using System.IO.Compression;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.Templates;

/// <summary>
/// Default <see cref="ITemplateService"/>. Stores each template under
/// <c>%AppData%\CM.EDITAR+\Templates\&lt;id&gt;\</c> with <c>metadata.json</c> and an optional body file.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    public string TemplatesDirectory { get; }

    public TemplateService(string? root = null)
    {
        TemplatesDirectory = root ?? AppPaths.TemplatesDir;
        Directory.CreateDirectory(TemplatesDirectory);
    }

    public async Task<IReadOnlyList<TemplateMetadata>> ListAsync(CancellationToken ct = default)
    {
        var results = new List<TemplateMetadata>();
        if (!Directory.Exists(TemplatesDirectory)) return results;
        foreach (var dir in Directory.EnumerateDirectories(TemplatesDirectory))
        {
            var metaPath = Path.Combine(dir, "metadata.json");
            if (!File.Exists(metaPath)) continue;
            try
            {
                var json = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
                var meta = JsonSerializer.Deserialize<TemplateMetadata>(json, JsonOpts);
                if (meta is not null) results.Add(meta);
            }
            catch { /* corrupt template; skip */ }
        }
        return results.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
    }

    public async Task<TemplateMetadata?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var metaPath = Path.Combine(DirFor(id), "metadata.json");
        if (!File.Exists(metaPath)) return null;
        var json = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TemplateMetadata>(json, JsonOpts);
    }

    public async Task<TemplateMetadata> CreateAsync(TemplateMetadata template, byte[]? body, CancellationToken ct = default)
    {
        var sanity = CommandSanitizer.Inspect(template);
        if (!sanity.Allowed) throw new InvalidOperationException(sanity.Reason);

        var fixedTemplate = template with
        {
            Id = template.Id == Guid.Empty ? Guid.NewGuid() : template.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        await PersistAsync(fixedTemplate, body, ct).ConfigureAwait(false);
        return fixedTemplate;
    }

    public async Task<TemplateMetadata> UpdateAsync(TemplateMetadata template, byte[]? body, CancellationToken ct = default)
    {
        var sanity = CommandSanitizer.Inspect(template);
        if (!sanity.Allowed) throw new InvalidOperationException(sanity.Reason);

        var updated = template with { ModifiedAt = DateTimeOffset.UtcNow };
        await PersistAsync(updated, body, ct).ConfigureAwait(false);
        return updated;
    }

    public Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dir = DirFor(id);
        if (!Directory.Exists(dir)) return Task.FromResult(OperationResult.Fail("Template not found."));
        try { Directory.Delete(dir, recursive: true); return Task.FromResult(OperationResult.Ok()); }
        catch (Exception ex) { return Task.FromResult(OperationResult.Fail(ex.Message, ex)); }
    }

    public async Task<byte[]> RenderAsync(Guid templateId, IReadOnlyDictionary<string, string>? overrides, CancellationToken ct = default)
    {
        var meta = await GetAsync(templateId, ct).ConfigureAwait(false)
            ?? throw new FileNotFoundException($"Template {templateId} not found.");

        byte[] body = meta.TemplateType switch
        {
            ShellNewType.NullFile => Array.Empty<byte>(),
            ShellNewType.Data => meta.DataBase64 is null ? Array.Empty<byte>() : Convert.FromBase64String(meta.DataBase64),
            ShellNewType.FileName => await LoadBodyAsync(meta, ct).ConfigureAwait(false),
            ShellNewType.Command => throw new InvalidOperationException("Command templates are resolved by FileCreator at runtime, not rendered."),
            _ => Array.Empty<byte>(),
        };
        return PlaceholderEngine.Resolve(body, meta, overrides);
    }

    public async Task<OperationResult> ExportPackAsync(IEnumerable<Guid> ids, string targetZipPath, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(targetZipPath)) File.Delete(targetZipPath);
            using var zip = ZipFile.Open(targetZipPath, ZipArchiveMode.Create);
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                var dir = DirFor(id);
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    var name = Path.Combine(id.ToString(), Path.GetFileName(file)).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, name);
                }
            }
            return OperationResult.Ok($"Exported pack to {targetZipPath}");
        }
        catch (Exception ex) { return OperationResult.Fail(ex.Message, ex); }
    }

    public async Task<OperationResult<IReadOnlyList<TemplateMetadata>>> ImportPackAsync(string sourceZipPath, CancellationToken ct = default)
    {
        try
        {
            using var zip = ZipFile.OpenRead(sourceZipPath);
            var imported = new List<TemplateMetadata>();
            foreach (var grouping in zip.Entries.GroupBy(e => e.FullName.Split('/')[0]))
            {
                if (!Guid.TryParse(grouping.Key, out var id)) continue;
                var dir = DirFor(id);
                Directory.CreateDirectory(dir);
                foreach (var entry in grouping)
                {
                    var dest = Path.Combine(dir, Path.GetFileName(entry.FullName));
                    entry.ExtractToFile(dest, overwrite: true);
                }
                var meta = await GetAsync(id, ct).ConfigureAwait(false);
                if (meta is null) continue;

                // Enforce the same command-template sanitizer gate at import time that
                // CreateAsync/UpdateAsync use. Quarantine unsafe Command templates by
                // resetting them to NullFile + CommandApproved=false and clearing the
                // command line so the operator must explicitly re-approve before any
                // downstream execution path can pick them up.
                var sanity = CommandSanitizer.Inspect(meta);
                if (!sanity.Allowed)
                {
                    meta = meta with
                    {
                        TemplateType = ShellNewType.NullFile,
                        TemplateSource = null,
                        CommandApproved = false,
                        ModifiedAt = DateTimeOffset.UtcNow,
                    };
                    await PersistAsync(meta, body: null, ct).ConfigureAwait(false);
                }
                imported.Add(meta);
            }
            return OperationResult<IReadOnlyList<TemplateMetadata>>.Ok(imported);
        }
        catch (Exception ex) { return OperationResult<IReadOnlyList<TemplateMetadata>>.Fail(ex.Message, ex); }
    }

    private async Task PersistAsync(TemplateMetadata meta, byte[]? body, CancellationToken ct)
    {
        var dir = DirFor(meta.Id);
        Directory.CreateDirectory(dir);
        var metaPath = Path.Combine(dir, "metadata.json");
        var json = JsonSerializer.Serialize(meta, JsonOpts);
        await File.WriteAllTextAsync(metaPath, json, ct).ConfigureAwait(false);

        if (body is not null && meta.TemplateType == ShellNewType.FileName)
        {
            var ext = meta.Extensions.FirstOrDefault() ?? ".bin";
            var bodyPath = Path.Combine(dir, "template" + ext);
            await File.WriteAllBytesAsync(bodyPath, body, ct).ConfigureAwait(false);
        }
    }

    private async Task<byte[]> LoadBodyAsync(TemplateMetadata meta, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(meta.TemplateSource) && File.Exists(meta.TemplateSource))
            return await File.ReadAllBytesAsync(meta.TemplateSource, ct).ConfigureAwait(false);

        var dir = DirFor(meta.Id);
        var first = Directory.EnumerateFiles(dir, "template.*").FirstOrDefault();
        return first is null ? Array.Empty<byte>() : await File.ReadAllBytesAsync(first, ct).ConfigureAwait(false);
    }

    private string DirFor(Guid id) => Path.Combine(TemplatesDirectory, id.ToString());

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
}
