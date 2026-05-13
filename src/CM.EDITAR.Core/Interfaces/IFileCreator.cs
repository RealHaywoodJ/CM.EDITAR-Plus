namespace CM.EDITAR.Core;

/// <summary>Atomically writes a file produced by a template. Used both in-process and via named-pipe RPC.</summary>
public interface IFileCreator
{
    Task<FileCreatorResponse> CreateAsync(FileCreatorRequest request, CancellationToken ct = default);
    Task<string> PreviewAsync(Guid templateId, IReadOnlyDictionary<string, string>? overrides, CancellationToken ct = default);
}

/// <summary>Per-install secret used to authenticate FileCreator IPC calls.</summary>
public interface ISecretStore
{
    Task<string> GetOrCreateTokenAsync(CancellationToken ct = default);
    Task<bool> ValidateAsync(string presented, CancellationToken ct = default);
}
