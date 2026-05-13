using System.Text;
using CM.EDITAR.Core;
using CM.EDITAR.Templates;

namespace CM.EDITAR.FileCreator;

/// <summary>Default <see cref="IFileCreator"/>. Resolves a template and writes it atomically.</summary>
public sealed class FileCreatorService : IFileCreator
{
    private readonly ITemplateService _templates;

    public FileCreatorService(ITemplateService templates) => _templates = templates;

    public async Task<FileCreatorResponse> CreateAsync(FileCreatorRequest request, CancellationToken ct = default)
    {
        var meta = await _templates.GetAsync(request.TemplateId, ct).ConfigureAwait(false);
        if (meta is null)
            return new FileCreatorResponse { Success = false, ErrorCode = "UNKNOWN_TEMPLATE", Diagnostics = $"Template {request.TemplateId} not found" };

        var sanity = CommandSanitizer.Inspect(meta);
        if (!sanity.Allowed)
            return new FileCreatorResponse { Success = false, ErrorCode = "REJECTED", Diagnostics = sanity.Reason };

        try
        {
            byte[] content = meta.TemplateType switch
            {
                ShellNewType.Command => Array.Empty<byte>(), // command templates are out-of-band; not handled here
                _ => await _templates.RenderAsync(request.TemplateId, request.PlaceholderOverrides, ct).ConfigureAwait(false),
            };

            // Refuse to overwrite — Explorer expects atomic non-clobbering create.
            if (File.Exists(request.TargetPath))
                return new FileCreatorResponse { Success = false, ErrorCode = "EXISTS", Diagnostics = $"Target already exists: {request.TargetPath}" };

            await AtomicWriter.WriteAsync(request.TargetPath, content, overwrite: false, ct).ConfigureAwait(false);
            return new FileCreatorResponse { Success = true, CreatedPath = request.TargetPath };
        }
        catch (Exception ex)
        {
            return new FileCreatorResponse { Success = false, ErrorCode = "WRITE_FAILED", Diagnostics = ex.Message };
        }
    }

    public async Task<string> PreviewAsync(Guid templateId, IReadOnlyDictionary<string, string>? overrides, CancellationToken ct = default)
    {
        var content = await _templates.RenderAsync(templateId, overrides, ct).ConfigureAwait(false);
        return Encoding.UTF8.GetString(content);
    }
}
