using CM.EDITAR.Core;
using CM.EDITAR.FileCreator;
using CM.EDITAR.Templates;

AppPaths.EnsureAll();

var templates = new TemplateService();
var secrets = new SecretStore();
var creator = new FileCreatorService(templates);

return await new CliRouter(args, creator, templates, secrets).RunAsync().ConfigureAwait(false);

internal sealed class CliRouter
{
    private readonly string[] _args;
    private readonly IFileCreator _creator;
    private readonly ITemplateService _templates;
    private readonly ISecretStore _secrets;

    public CliRouter(string[] args, IFileCreator creator, ITemplateService templates, ISecretStore secrets)
    {
        _args = args; _creator = creator; _templates = templates; _secrets = secrets;
    }

    public async Task<int> RunAsync()
    {
        if (_args.Length == 0 || HasFlag("--help") || HasFlag("-h")) { PrintHelp(); return 0; }

        // WiX MSI custom-action entry points (per spec: deferred + impersonate=yes).
        if (HasFlag("--installer-snapshot"))
        {
            var snapshots = new CM.EDITAR.Registry.SnapshotService();
            var keys = new[]
            {
                @"HKCU\Software\Classes",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts",
                @"HKCU\Software\CM.EDITAR+",
            };
            var meta = await snapshots.CreateAsync(keys, "installer-pre-install", manifestId: null).ConfigureAwait(false);
            Console.WriteLine($"Installer snapshot written: {meta.RegFilePath}");
            return 0;
        }
        if (HasFlag("--restore-installer-snapshot"))
        {
            var snapshots = new CM.EDITAR.Registry.SnapshotService();
            var all = await snapshots.ListAsync().ConfigureAwait(false);
            var target = all.FirstOrDefault(s => s.Reason == "installer-pre-install");
            if (target is null) { Console.WriteLine("No installer snapshot found; nothing to restore."); return 0; }
            var r = await snapshots.RestoreAsync(target).ConfigureAwait(false);
            Console.WriteLine(r.Message ?? (r.Success ? "Installer snapshot restored." : "Restore failed."));
            return r.Success ? 0 : 1;
        }

        if (HasFlag("--serve"))
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            var server = new PipeServer(_creator, _secrets);
            Console.WriteLine($"FileCreator listening on pipe \"{AppPaths.FileCreatorPipeName}\"...");
            await server.RunAsync(cts.Token).ConfigureAwait(false);
            return 0;
        }

        var templateId = GetOption("--template-id");
        if (string.IsNullOrEmpty(templateId)) { PrintHelp(); return 64; }
        if (!Guid.TryParse(templateId, out var id)) { Console.Error.WriteLine("Invalid --template-id"); return 64; }

        if (HasFlag("--preview"))
        {
            var preview = await _creator.PreviewAsync(id, overrides: null, CancellationToken.None).ConfigureAwait(false);
            Console.Out.Write(preview);
            return 0;
        }

        if (HasFlag("--create"))
        {
            var target = GetOption("--target");
            if (string.IsNullOrEmpty(target)) { Console.Error.WriteLine("--create requires --target"); return 64; }

            var token = await _secrets.GetOrCreateTokenAsync().ConfigureAwait(false);
            var resp = await _creator.CreateAsync(new FileCreatorRequest
            {
                TemplateId = id, TargetPath = target!, Token = token,
            }).ConfigureAwait(false);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(resp));
            return resp.Success ? 0 : 1;
        }

        PrintHelp();
        return 64;
    }

    private bool HasFlag(string flag) => _args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

    private string? GetOption(string name)
    {
        for (int i = 0; i < _args.Length - 1; i++)
            if (_args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return _args[i + 1];
        return null;
    }

    private static void PrintHelp() => Console.WriteLine("""
        CM.EDITAR.FileCreator — non-elevated template runtime

        USAGE
          CM.EDITAR.FileCreator --create  --template-id <guid> --target "<path>"
          CM.EDITAR.FileCreator --preview --template-id <guid>
          CM.EDITAR.FileCreator --serve

        Notes:
          --create writes atomically (temp-file + rename) and refuses to overwrite.
          --serve starts the named-pipe server with DPAPI-protected token auth.
        """);
}
