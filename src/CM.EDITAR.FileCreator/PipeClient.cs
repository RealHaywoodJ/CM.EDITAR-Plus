using System.IO.Pipes;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.FileCreator;

/// <summary>Client helper for talking to <see cref="PipeServer"/> from the UI or other processes.</summary>
public static class PipeClient
{
    public static async Task<FileCreatorResponse> SendAsync(FileCreatorRequest request, string? pipeName = null, int timeoutMs = 10_000, CancellationToken ct = default)
    {
        var name = pipeName ?? AppPaths.FileCreatorPipeName;
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(timeoutMs, ct).ConfigureAwait(false);

        await using (var writer = new StreamWriter(client, leaveOpen: true) { AutoFlush = true })
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), ct).ConfigureAwait(false);
        }
        using var reader = new StreamReader(client, leaveOpen: true);
        var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (line is null) throw new IOException("Empty response from FileCreator pipe.");
        return JsonSerializer.Deserialize<FileCreatorResponse>(line)
            ?? throw new IOException("Unparseable response from FileCreator pipe.");
    }
}
