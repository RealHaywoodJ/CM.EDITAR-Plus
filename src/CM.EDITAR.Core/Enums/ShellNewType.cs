namespace CM.EDITAR.Core;

/// <summary>
/// Defines the four ShellNew variants the Windows Explorer "New" submenu honors.
/// See https://learn.microsoft.com/windows/win32/shell/context for reference.
/// </summary>
public enum ShellNewType
{
    /// <summary>Creates an empty file with the registered extension.</summary>
    NullFile = 0,
    /// <summary>Copies a static template file from a path on disk.</summary>
    FileName = 1,
    /// <summary>Runs a command line that produces the file.</summary>
    Command = 2,
    /// <summary>Stores raw bytes inline in the registry value.</summary>
    Data = 3,
}
