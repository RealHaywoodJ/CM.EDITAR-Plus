using System.Text.RegularExpressions;
using CM.EDITAR.Core;

namespace CM.EDITAR.Templates;

/// <summary>
/// Resolves placeholders inside template bodies. Built-ins: %DATE%, %TIME%, %USERNAME%, %YEAR%, %GUID%.
/// Custom placeholders defined per-template take precedence when overrides are supplied.
/// </summary>
public static class PlaceholderEngine
{
    private static readonly Regex PlaceholderPattern = new(@"%(?<name>[A-Za-z0-9_\-]+)%", RegexOptions.Compiled);

    public static string Resolve(string body, TemplateMetadata template, IReadOnlyDictionary<string, string>? overrides = null, DateTimeOffset? now = null)
    {
        var stamp = now ?? DateTimeOffset.Now;
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DATE"] = stamp.ToString("yyyy-MM-dd"),
            ["TIME"] = stamp.ToString("HH:mm:ss"),
            ["USERNAME"] = Environment.UserName,
            ["YEAR"] = stamp.Year.ToString(),
            ["GUID"] = Guid.NewGuid().ToString("D"),
        };

        foreach (var p in template.Placeholders)
            if (p.Default is not null && !defaults.ContainsKey(p.Name))
                defaults[p.Name] = p.Default;

        if (overrides is not null)
            foreach (var kv in overrides) defaults[kv.Key] = kv.Value;

        return PlaceholderPattern.Replace(body, m =>
        {
            var name = m.Groups["name"].Value;
            return defaults.TryGetValue(name, out var v) ? v : m.Value;
        });
    }

    public static byte[] Resolve(byte[] body, TemplateMetadata template, IReadOnlyDictionary<string, string>? overrides = null)
    {
        // Only resolve placeholders for text-like content. Binary (Data) templates pass through unchanged.
        if (template.TemplateType == ShellNewType.Data) return body;
        var text = System.Text.Encoding.UTF8.GetString(body);
        return System.Text.Encoding.UTF8.GetBytes(Resolve(text, template, overrides));
    }
}
