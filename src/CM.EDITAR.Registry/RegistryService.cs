using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using CM.EDITAR.Core;
using Win32 = Microsoft.Win32;

namespace CM.EDITAR.Registry;

/// <summary>
/// Default <see cref="IRegistryService"/> implementation. The Windows-only branches use
/// <see cref="Win32.Registry"/>; on non-Windows platforms the service throws on writes
/// but discovery returns an empty list (so the project remains cross-compilable).
/// </summary>
public sealed class RegistryService : IRegistryService
{
    private readonly ProgIdResolver _progIdResolver;

    public RegistryService(ProgIdResolver? resolver = null)
    {
        _progIdResolver = resolver ?? new ProgIdResolver();
    }

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public Task<IReadOnlyList<ShellNewEntry>> DiscoverAsync(CancellationToken ct = default)
    {
        if (!IsSupported)
        {
            return Task.FromResult<IReadOnlyList<ShellNewEntry>>(Array.Empty<ShellNewEntry>());
        }
        return Task.FromResult(DiscoverWindows(ct));
    }

    [SupportedOSPlatform("windows")]
    private IReadOnlyList<ShellNewEntry> DiscoverWindows(CancellationToken ct)
    {
        var found = new Dictionary<string, ShellNewEntry>(StringComparer.OrdinalIgnoreCase);

        // HKCU first (authoritative for the user).
        ScanHive(Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes"), RegistryHive.HKCU, found, ct);

        // HKCR fallback for discovery only — entries already in HKCU win.
        ScanHive(Win32.Registry.ClassesRoot, RegistryHive.HKCR, found, ct);

        return found.Values.OrderBy(e => e.Group).ThenBy(e => e.Extension).ToList();
    }

    [SupportedOSPlatform("windows")]
    private void ScanHive(Win32.RegistryKey? root, RegistryHive hive, Dictionary<string, ShellNewEntry> sink, CancellationToken ct)
    {
        if (root is null) return;

        foreach (var subName in root.GetSubKeyNames())
        {
            ct.ThrowIfCancellationRequested();
            if (!subName.StartsWith('.')) continue; // ShellNew lives under .ext keys

            using var extKey = root.OpenSubKey(subName);
            if (extKey is null) continue;

            // Direct .ext\ShellNew
            using (var sn = extKey.OpenSubKey("ShellNew"))
            {
                if (sn is not null)
                {
                    var entry = ReadEntry(subName, sn, hive, progId: null);
                    if (entry is not null && !sink.ContainsKey(subName))
                        sink[subName] = entry;
                }
            }

            // Resolve ProgID and check ProgID\ShellNew
            var progId = (extKey.GetValue(null) as string)?.Trim();
            if (!string.IsNullOrEmpty(progId))
            {
                using var progIdKey = (hive == RegistryHive.HKCU
                    ? Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}")
                    : Win32.Registry.ClassesRoot.OpenSubKey(progId));
                using var sn = progIdKey?.OpenSubKey("ShellNew");
                if (sn is not null && !sink.ContainsKey(subName))
                {
                    var entry = ReadEntry(subName, sn, hive, progId);
                    if (entry is not null) sink[subName] = entry;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool HasUserChoice(string ext)
    {
        try
        {
            using var k = Win32.Registry.CurrentUser.OpenSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext}\UserChoice");
            return k is not null && (k.GetValue("ProgId") as string) is { Length: > 0 };
        }
        catch { return false; }
    }

    [SupportedOSPlatform("windows")]
    private static ShellNewEntry? ReadEntry(string ext, Win32.RegistryKey shellNewKey, RegistryHive hive, string? progId)
    {
        var fileName = shellNewKey.GetValue("FileName") as string;
        var command = shellNewKey.GetValue("Command") as string;
        var data = shellNewKey.GetValue("Data") as byte[];
        var hasNullFile = shellNewKey.GetValueNames().Any(n => n.Equals("NullFile", StringComparison.OrdinalIgnoreCase));
        var itemName = shellNewKey.GetValue("ItemName") as string;

        ShellNewType type =
            !string.IsNullOrEmpty(fileName) ? ShellNewType.FileName :
            !string.IsNullOrEmpty(command) ? ShellNewType.Command :
            data is { Length: > 0 } ? ShellNewType.Data :
            hasNullFile ? ShellNewType.NullFile :
            ShellNewType.NullFile;

        var visible = hasNullFile || !string.IsNullOrEmpty(fileName) || !string.IsNullOrEmpty(command) || (data?.Length ?? 0) > 0;

        return new ShellNewEntry
        {
            Extension = ext,
            DisplayName = itemName ?? PrettyName(ext),
            ShellNewType = type,
            FileName = fileName,
            Command = command,
            DataBase64 = data is { Length: > 0 } ? Convert.ToBase64String(data) : null,
            Visible = visible,
            SourceKey = shellNewKey.Name,
            SourceHive = hive,
            ProgIdResolved = progId,
            HasUserChoice = HasUserChoice(ext),
            Risk = RiskScorer.Score(ext, type, command),
            State = visible ? EntryState.Enabled : EntryState.Disabled,
            Group = GroupFor(ext),
            Pack = "system",
        };
    }

    private static string PrettyName(string ext) =>
        ext.Length > 1 ? $"{char.ToUpperInvariant(ext[1])}{ext[2..]} Document" : "New File";

    private static string GroupFor(string ext) => ext.ToLowerInvariant() switch
    {
        ".txt" or ".md" or ".rtf" or ".csv" or ".log" or ".json" or ".xml" or ".yaml" or ".yml" => "Text/Data",
        ".docx" or ".xlsx" or ".pptx" or ".odt" or ".ods" or ".odp" => "Office/Docs",
        ".html" or ".css" or ".js" or ".ts" => "Web/Code",
        ".py" or ".rb" or ".ps1" or ".bat" or ".cmd" or ".sh" => "Power User",
        ".zip" or ".7z" or ".rar" or ".tar" or ".gz" => "Archives",
        _ => "Other",
    };

    public Task<ProgIdResolution> ResolveProgIdAsync(string extension, CancellationToken ct = default) =>
        Task.FromResult(_progIdResolver.Resolve(extension));

    public Task<ApplyManifest> BuildManifestAsync(IEnumerable<StagedChange> staged, CancellationToken ct = default)
    {
        var ops = new List<RegistryOperation>();
        bool requiresTyped = false;

        foreach (var change in staged)
        {
            ct.ThrowIfCancellationRequested();
            var resolution = _progIdResolver.Resolve(change.Extension);

            // We always write under HKCU. If a UserChoice exists we must not overwrite it; we still write
            // the ShellNew under the resolved ProgID path (which Explorer will honor for opening), but warn the user.
            var keyPath = resolution.ResolvedShellNewKeyPath;

            switch (change.Action)
            {
                case StagedAction.Enable or StagedAction.Add or StagedAction.Edit:
                    // Mutual exclusion: Explorer's ShellNew honors exactly ONE of {NullFile,
                    // FileName, Command, Data}. When switching target type we must delete
                    // any conflicting legacy values so the resulting state is unambiguous.
                    foreach (var stale in ConflictingValuesFor(change.TargetType))
                    {
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.DeleteValue,
                            KeyPath = keyPath, ValueName = stale,
                            Comment = $"Remove conflicting ShellNew value (target type={change.TargetType})",
                        });
                    }

                    // Always ensure NullFile exists for visibility in the modern Windows 11 New menu.
                    if (change.TargetType is ShellNewType.NullFile or ShellNewType.FileName
                        or ShellNewType.Command or ShellNewType.Data)
                    {
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.Add,
                            KeyPath = keyPath,
                            ValueName = "NullFile",
                            ValueKind = "REG_SZ",
                            Value = "",
                            Comment = "Force visibility in modern Windows 11 New menu",
                        });
                    }

                    if (!string.IsNullOrEmpty(change.DisplayName))
                    {
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.Modify,
                            KeyPath = keyPath,
                            ValueName = "ItemName",
                            ValueKind = "REG_SZ",
                            Value = change.DisplayName,
                        });
                    }
                    if (change.TargetType == ShellNewType.FileName && !string.IsNullOrEmpty(change.FileName))
                    {
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.Modify,
                            KeyPath = keyPath, ValueName = "FileName",
                            ValueKind = "REG_SZ", Value = change.FileName,
                        });
                    }
                    if (change.TargetType == ShellNewType.Command && !string.IsNullOrEmpty(change.Command))
                    {
                        requiresTyped = true;
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.Modify,
                            KeyPath = keyPath, ValueName = "Command",
                            ValueKind = "REG_SZ", Value = change.Command,
                            Comment = "COMMAND template — high risk; requires explicit user approval",
                        });
                    }
                    if (change.TargetType == ShellNewType.Data && !string.IsNullOrEmpty(change.DataBase64))
                    {
                        // Data ShellNew: inline byte payload Explorer writes verbatim into the new file.
                        // Stored as REG_BINARY; ApplyOperation hex-decodes the value for binary kinds.
                        var hex = Convert.ToHexString(Convert.FromBase64String(change.DataBase64));
                        ops.Add(new RegistryOperation
                        {
                            Kind = RegistryOperationKind.Modify,
                            KeyPath = keyPath, ValueName = "Data",
                            ValueKind = "REG_BINARY", Value = hex,
                            Comment = "Inline ShellNew Data payload",
                        });
                    }
                    break;

                case StagedAction.Disable or StagedAction.Remove:
                    ops.Add(new RegistryOperation
                    {
                        Kind = RegistryOperationKind.Delete,
                        KeyPath = keyPath,
                        Comment = "Hide entry from New submenu by deleting ShellNew subkey",
                    });
                    break;
            }
        }

        return Task.FromResult(new ApplyManifest
        {
            Operations = ops,
            RequiresTypedConfirmation = requiresTyped,
            RequiresElevation = false,
            Notes = "All operations target HKCU. No HKLM writes scheduled.",
        });
    }

    /// <summary>The set of ShellNew payload values that must be removed when switching to <paramref name="target"/>.</summary>
    private static IEnumerable<string> ConflictingValuesFor(ShellNewType target) => target switch
    {
        ShellNewType.FileName => new[] { "Command", "Data" },
        ShellNewType.Command  => new[] { "FileName", "Data" },
        ShellNewType.Data     => new[] { "FileName", "Command" },
        ShellNewType.NullFile => new[] { "FileName", "Command", "Data" },
        _ => Array.Empty<string>(),
    };

    public void NotifyShellOfChange()
    {
        if (!IsSupported) return;
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_ASSOCCHANGED,
            NativeMethods.SHCNF_IDLIST | NativeMethods.SHCNF_FLUSH,
            IntPtr.Zero, IntPtr.Zero);
    }

    public Task<bool> VerifyManifestAsync(ApplyManifest manifest, CancellationToken ct = default)
    {
        if (!IsSupported) return Task.FromResult(false);
        return Task.FromResult(VerifyWindows(manifest, ct));
    }

    [SupportedOSPlatform("windows")]
    private static bool VerifyWindows(ApplyManifest manifest, CancellationToken ct)
    {
        foreach (var op in manifest.Operations)
        {
            ct.ThrowIfCancellationRequested();
            var (root, sub) = SplitHkcuPath(op.KeyPath);
            if (root is null) continue;
            using var key = root.OpenSubKey(sub);
            switch (op.Kind)
            {
                case RegistryOperationKind.Delete:
                    if (key is not null) return false;
                    break;
                case RegistryOperationKind.Add or RegistryOperationKind.Modify:
                    if (key is null) return false;
                    // Empty ValueName is the registry default value (`@`) and must still be verified.
                    var lookupName = string.IsNullOrEmpty(op.ValueName) ? null : op.ValueName;
                    var v = key.GetValue(lookupName);
                    if (v is null) return false;
                    if (op.Value is not null && !ValueMatches(v, op))
                        return false;
                    break;
                case RegistryOperationKind.DeleteValue:
                    // Cleanup ops succeed when either the key is gone or the named value is absent.
                    if (key is not null && key.GetValue(
                            string.IsNullOrEmpty(op.ValueName) ? null : op.ValueName) is not null)
                        return false;
                    break;
            }
        }
        return true;
    }

    /// <summary>
    /// Value-kind aware comparison between a stored registry value and a manifest-expected string.
    /// REG_BINARY is stored as a byte[] and the manifest carries hex pairs; everything else falls
    /// back to invariant string compare.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static bool ValueMatches(object stored, RegistryOperation op)
    {
        if (op.ValueKind == "REG_BINARY")
        {
            if (stored is not byte[] bytes) return false;
            var actualHex = Convert.ToHexString(bytes);
            return string.Equals(actualHex, op.Value, StringComparison.OrdinalIgnoreCase);
        }
        if (op.ValueKind == "REG_DWORD")
        {
            return stored is int i && string.Equals(i.ToString(), op.Value, StringComparison.Ordinal);
        }
        return string.Equals(stored.ToString(), op.Value, StringComparison.Ordinal);
    }

    [SupportedOSPlatform("windows")]
    internal static (Win32.RegistryKey? root, string sub) SplitHkcuPath(string fullPath)
    {
        const string prefix = "HKCU\\";
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return (null, "");
        return (Win32.Registry.CurrentUser, fullPath[prefix.Length..]);
    }

    /// <summary>Apply a single operation. INTERNAL: only callable from <see cref="ApplyService"/>.</summary>
    [SupportedOSPlatform("windows")]
    internal static void ApplyOperation(RegistryOperation op)
    {
        if (!op.KeyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Refusing non-HKCU write: {op.KeyPath}");

        var (_, sub) = SplitHkcuPath(op.KeyPath);
        switch (op.Kind)
        {
            case RegistryOperationKind.Delete:
                Win32.Registry.CurrentUser.DeleteSubKeyTree(sub, throwOnMissingSubKey: false);
                break;
            case RegistryOperationKind.DeleteValue:
                using (var delKey = Win32.Registry.CurrentUser.OpenSubKey(sub, writable: true))
                {
                    // Best-effort: if the key or value doesn't exist, this is a no-op.
                    delKey?.DeleteValue(op.ValueName, throwOnMissingValue: false);
                }
                break;
            case RegistryOperationKind.Add or RegistryOperationKind.Modify:
                using (var key = Win32.Registry.CurrentUser.CreateSubKey(sub, writable: true))
                {
                    if (key is null) throw new InvalidOperationException($"Cannot open key: {op.KeyPath}");
                    var kind = op.ValueKind switch
                    {
                        "REG_BINARY" => Win32.RegistryValueKind.Binary,
                        "REG_DWORD" => Win32.RegistryValueKind.DWord,
                        "REG_EXPAND_SZ" => Win32.RegistryValueKind.ExpandString,
                        "REG_NONE" => Win32.RegistryValueKind.None,
                        _ => Win32.RegistryValueKind.String,
                    };
                    object value = kind == Win32.RegistryValueKind.Binary
                        ? Convert.FromHexString(op.Value ?? "")
                        : (object)(op.Value ?? "");
                    // Empty ValueName writes the default value (`@` in .reg syntax).
                    var writeName = string.IsNullOrEmpty(op.ValueName) ? null : op.ValueName;
                    key.SetValue(writeName, value, kind);
                }
                break;
        }
    }

    /// <summary>
    /// Export the given keys to a .reg file. Faithfully serializes every supported value kind in the
    /// standard Windows Registry Editor 5.00 (UTF-16 LE) format so a subsequent <c>regedit /s</c>
    /// restore round-trips byte-exactly. Recurses into subkeys.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static byte[] ExportKeysToReg(IEnumerable<string> keys)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();
        foreach (var key in keys)
        {
            var (_, sub) = SplitHkcuPath(key);
            using var k = Win32.Registry.CurrentUser.OpenSubKey(sub);
            if (k is null)
            {
                sb.AppendLine($"[-{key}]");
                sb.AppendLine();
                continue;
            }
            WriteKeyRecursive(sb, k, key);
        }
        return Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(sb.ToString())).ToArray();
    }

    [SupportedOSPlatform("windows")]
    private static void WriteKeyRecursive(StringBuilder sb, Win32.RegistryKey key, string fullPath)
    {
        sb.AppendLine($"[{fullPath}]");
        foreach (var name in key.GetValueNames())
        {
            var kind = key.GetValueKind(name);
            var raw = key.GetValue(name, defaultValue: null,
                Win32.RegistryValueOptions.DoNotExpandEnvironmentNames);
            sb.AppendLine(FormatRegValue(name, kind, raw));
        }
        sb.AppendLine();
        foreach (var sub in key.GetSubKeyNames())
        {
            using var child = key.OpenSubKey(sub);
            if (child is not null) WriteKeyRecursive(sb, child, $"{fullPath}\\{sub}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static string FormatRegValue(string name, Win32.RegistryValueKind kind, object? raw)
    {
        var nameToken = string.IsNullOrEmpty(name) ? "@" : $"\"{Escape(name)}\"";
        return kind switch
        {
            Win32.RegistryValueKind.String =>
                $"{nameToken}=\"{Escape(raw as string ?? "")}\"",
            Win32.RegistryValueKind.ExpandString =>
                $"{nameToken}=hex(2):{HexBytes(Encoding.Unicode.GetBytes((raw as string ?? "") + "\0"))}",
            Win32.RegistryValueKind.Binary =>
                $"{nameToken}=hex:{HexBytes(raw as byte[] ?? Array.Empty<byte>())}",
            Win32.RegistryValueKind.DWord =>
                $"{nameToken}=dword:{Convert.ToUInt32(raw):x8}",
            Win32.RegistryValueKind.QWord =>
                $"{nameToken}=hex(b):{HexBytes(BitConverter.GetBytes(Convert.ToUInt64(raw)))}",
            Win32.RegistryValueKind.MultiString =>
                $"{nameToken}=hex(7):{HexBytes(MultiStringToBytes((raw as string[]) ?? Array.Empty<string>()))}",
            Win32.RegistryValueKind.None =>
                $"{nameToken}=hex(0):{HexBytes(raw as byte[] ?? Array.Empty<byte>())}",
            _ => $"; unsupported kind {kind} for {nameToken}",
        };
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string HexBytes(byte[] data)
    {
        if (data.Length == 0) return "";
        var sb = new StringBuilder(data.Length * 3);
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(data[i].ToString("x2"));
        }
        return sb.ToString();
    }

    private static byte[] MultiStringToBytes(string[] values)
    {
        var ms = new MemoryStream();
        foreach (var v in values)
        {
            var bytes = Encoding.Unicode.GetBytes(v + "\0");
            ms.Write(bytes, 0, bytes.Length);
        }
        ms.Write(new byte[] { 0, 0 }, 0, 2); // double-null terminator
        return ms.ToArray();
    }

    /// <summary>Lookup the current user's SID for snapshot/audit metadata.</summary>
    public static string GetCurrentUserSid()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "S-0-0-0";
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? "S-0-0-0";
        }
        catch { return "S-0-0-0"; }
    }
}
