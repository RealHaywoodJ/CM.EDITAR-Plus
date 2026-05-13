namespace CM.EDITAR.Core;

/// <summary>Manages templates on disk and resolves placeholders into rendered file content.</summary>
public interface ITemplateService
{
    Task<IReadOnlyList<TemplateMetadata>> ListAsync(CancellationToken ct = default);
    Task<TemplateMetadata?> GetAsync(Guid id, CancellationToken ct = default);
    Task<TemplateMetadata> CreateAsync(TemplateMetadata template, byte[]? body, CancellationToken ct = default);
    Task<TemplateMetadata> UpdateAsync(TemplateMetadata template, byte[]? body, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<byte[]> RenderAsync(Guid templateId, IReadOnlyDictionary<string, string>? overrides, CancellationToken ct = default);

    Task<OperationResult> ExportPackAsync(IEnumerable<Guid> ids, string targetZipPath, CancellationToken ct = default);
    Task<OperationResult<IReadOnlyList<TemplateMetadata>>> ImportPackAsync(string sourceZipPath, CancellationToken ct = default);

    /// <summary>Root directory where templates are persisted (typically %AppData%\CM.EDITAR\Templates).</summary>
    string TemplatesDirectory { get; }
}
