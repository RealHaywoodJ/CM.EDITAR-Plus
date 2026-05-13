using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CM.EDITAR.Core;
using Win32 = Microsoft.Win32;

namespace CM.EDITAR.Registry;

/// <summary>
/// Resolves the effective ProgID for an extension following the Windows shell precedence:
/// UserChoice → HKCU\Software\Classes\.ext → HKCR\.ext.
/// Per spec: never overwrite UserChoice; always write ShellNew under the resolved ProgID path.
/// </summary>
public sealed class ProgIdResolver
{
    public ProgIdResolution Resolve(string extension)
    {
        if (!extension.StartsWith('.'))
            throw new ArgumentException("Extension must start with '.'", nameof(extension));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProgIdResolution(extension, null, false, false,
                $@"HKCU\Software\Classes\{extension}\ShellNew");
        }
        return ResolveWindows(extension);
    }

    [SupportedOSPlatform("windows")]
    private static ProgIdResolution ResolveWindows(string extension)
    {
        // 1. UserChoice (Explorer's authoritative choice for opening; we honor but never overwrite it)
        using (var userChoice = Win32.Registry.CurrentUser.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\UserChoice"))
        {
            if (userChoice is not null)
            {
                var progId = userChoice.GetValue("ProgId") as string;
                if (!string.IsNullOrEmpty(progId))
                {
                    return new ProgIdResolution(extension, progId, FromUserChoice: true, FromHkcuOverride: false,
                        ResolvedShellNewKeyPath: $@"HKCU\Software\Classes\{progId}\ShellNew");
                }
            }
        }

        // 2. HKCU override of .ext default
        using (var hkcuExt = Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}"))
        {
            var progId = hkcuExt?.GetValue(null) as string;
            if (!string.IsNullOrEmpty(progId))
            {
                return new ProgIdResolution(extension, progId, FromUserChoice: false, FromHkcuOverride: true,
                    ResolvedShellNewKeyPath: $@"HKCU\Software\Classes\{progId}\ShellNew");
            }
        }

        // 3. HKCR fallback
        using (var hkcrExt = Win32.Registry.ClassesRoot.OpenSubKey(extension))
        {
            var progId = hkcrExt?.GetValue(null) as string;
            if (!string.IsNullOrEmpty(progId))
            {
                return new ProgIdResolution(extension, progId, FromUserChoice: false, FromHkcuOverride: false,
                    ResolvedShellNewKeyPath: $@"HKCU\Software\Classes\{progId}\ShellNew");
            }
        }

        // 4. No ProgID — write ShellNew under .ext directly (still HKCU)
        return new ProgIdResolution(extension, null, false, false,
            $@"HKCU\Software\Classes\{extension}\ShellNew");
    }
}
