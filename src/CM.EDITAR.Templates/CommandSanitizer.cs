using CM.EDITAR.Core;

namespace CM.EDITAR.Templates;

/// <summary>
/// Sanitization gate for Command templates. Static templates always pass.
/// Command templates must be explicitly approved AND must not reference disallowed surfaces.
/// </summary>
public static class CommandSanitizer
{
    private static readonly string[] DisallowedTokens =
    {
        "\\\\", "//", "http://", "https://", "ftp://", "smb:", "\\\\?\\UNC",
        "msiexec", "powershell.exe -enc", "iex(", "Invoke-Expression",
        "downloadstring", "downloadfile", "Start-BitsTransfer",
    };

    public sealed record SanitizationResult(bool Allowed, string? Reason);

    public static SanitizationResult Inspect(TemplateMetadata template)
    {
        if (template.TemplateType != ShellNewType.Command) return new(true, null);
        if (!template.CommandApproved) return new(false, "Command templates require explicit user approval (commandApproved=true).");
        if (string.IsNullOrWhiteSpace(template.TemplateSource)) return new(false, "Command template has no command line.");

        var src = template.TemplateSource;
        foreach (var token in DisallowedTokens)
        {
            if (src.Contains(token, StringComparison.OrdinalIgnoreCase))
                return new(false, $"Command contains disallowed token: \"{token}\".");
        }
        return new(true, null);
    }
}
