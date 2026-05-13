using CM.EDITAR.Core;

namespace CM.EDITAR.Registry;

/// <summary>Pure heuristic that classifies an entry as Recommended / Warning / High risk.</summary>
public static class RiskScorer
{
    private static readonly HashSet<string> KnownSafeData = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".rtf", ".csv", ".log", ".json", ".xml", ".yaml", ".yml",
        ".html", ".css", ".docx", ".xlsx", ".pptx", ".odt", ".ods", ".odp",
        ".zip", ".7z", ".rar",
    };

    private static readonly HashSet<string> KnownExecutable = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat", ".cmd", ".ps1", ".vbs", ".js", ".wsf", ".jse", ".scr", ".exe", ".msi", ".reg",
    };

    public static RiskLevel Score(string extension, ShellNewType type, string? command)
    {
        if (type == ShellNewType.Command) return RiskLevel.High;
        if (KnownExecutable.Contains(extension)) return RiskLevel.High;
        if (!string.IsNullOrWhiteSpace(command)) return RiskLevel.High;
        if (extension.Equals(".py", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".rb", StringComparison.OrdinalIgnoreCase)) return RiskLevel.Warning;
        if (KnownSafeData.Contains(extension)) return RiskLevel.Recommended;
        return RiskLevel.Warning;
    }
}
