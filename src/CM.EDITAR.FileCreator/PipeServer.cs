using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using CM.EDITAR.Core;

namespace CM.EDITAR.FileCreator;

/// <summary>
/// Named pipe server for the FileCreator runtime. Accepts JSON requests, validates the per-install
/// token via <see cref="ISecretStore"/>, and dispatches to <see cref="IFileCreator"/>. The pipe ACL
/// is restricted to the current user.
/// </summary>
public sealed class PipeServer
{
    private readonly IFileCreator _creator;
    private readonly ISecretStore _secrets;
    private readonly string _pipeName;
    private const int MaxRequestsPerSecond = 10;
    private readonly SemaphoreSlim _throttle = new(MaxRequestsPerSecond, MaxRequestsPerSecond);

    public PipeServer(IFileCreator creator, ISecretStore secrets, string? pipeName = null)
    {
        _creator = creator;
        _secrets = secrets;
        _pipeName = pipeName ?? AppPaths.FileCreatorPipeName;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var server = CreateServerStream();
            try
            {
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { server.Dispose(); break; }
            catch { server.Dispose(); throw; }

            _ = Task.Run(async () =>
            {
                try { await HandleAsync(server, ct).ConfigureAwait(false); }
                finally { server.Dispose(); }
            }, ct);
        }
    }

    // Allow concurrent server instances so RunAsync can immediately listen for the next
    // client while a prior connection is still being handled on a background task.
    // Using a single instance (max=1) caused "all pipe instances busy" IOExceptions
    // after the first request.
    private const int MaxConcurrentInstances = NamedPipeServerStream.MaxAllowedServerInstances;

    private NamedPipeServerStream CreateServerStream()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new NamedPipeServerStream(_pipeName, PipeDirection.InOut, MaxConcurrentInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
        return CreateWindowsServerStream();
    }

    [SupportedOSPlatform("windows")]
    private NamedPipeServerStream CreateWindowsServerStream()
    {
        var security = new PipeSecurity();
        var sid = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Cannot determine current user SID for pipe ACL.");
        security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return NamedPipeServerStreamAcl.Create(_pipeName, PipeDirection.InOut, MaxConcurrentInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, security);
    }

    private async Task HandleAsync(NamedPipeServerStream stream, CancellationToken ct)
    {
        if (!await _throttle.WaitAsync(0, ct).ConfigureAwait(false))
        {
            await WriteResponseAsync(stream, new FileCreatorResponse
            {
                Success = false, ErrorCode = "RATE_LIMITED", Diagnostics = "Too many requests"
            }, ct).ConfigureAwait(false);
            return;
        }
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) return;

            FileCreatorRequest? req;
            try { req = JsonSerializer.Deserialize<FileCreatorRequest>(line); }
            catch { req = null; }

            if (req is null)
            {
                await WriteResponseAsync(stream, new FileCreatorResponse
                {
                    Success = false, ErrorCode = "BAD_REQUEST", Diagnostics = "Malformed JSON"
                }, ct).ConfigureAwait(false);
                return;
            }

            if (!await _secrets.ValidateAsync(req.Token, ct).ConfigureAwait(false))
            {
                await WriteResponseAsync(stream, new FileCreatorResponse
                {
                    Success = false, ErrorCode = "UNAUTHORIZED", Diagnostics = "Invalid token"
                }, ct).ConfigureAwait(false);
                return;
            }

            var resp = await _creator.CreateAsync(req, ct).ConfigureAwait(false);
            await WriteResponseAsync(stream, resp, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteResponseAsync(stream, new FileCreatorResponse
            {
                Success = false, ErrorCode = "INTERNAL", Diagnostics = ex.Message
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            _ = Task.Delay(1000, CancellationToken.None).ContinueWith(_ => _throttle.Release(), TaskScheduler.Default);
        }
    }

    private static async Task WriteResponseAsync(NamedPipeServerStream stream, FileCreatorResponse resp, CancellationToken ct)
    {
        await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(resp).AsMemory(), ct).ConfigureAwait(false);
    }
}
