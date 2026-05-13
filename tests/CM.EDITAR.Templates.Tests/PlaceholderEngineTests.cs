using CM.EDITAR.Core;
using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Templates.Tests;

public class PlaceholderEngineTests
{
    private static TemplateMetadata MakeTemplate(params PlaceholderSpec[] placeholders) => new()
    {
        Id = Guid.NewGuid(),
        Name = "test",
        Placeholders = placeholders,
        TemplateType = ShellNewType.FileName,
    };

    [Fact]
    public void Resolve_ReplacesBuiltInPlaceholders()
    {
        var t = MakeTemplate();
        var now = new DateTimeOffset(2026, 5, 12, 10, 30, 0, TimeSpan.Zero);
        var result = PlaceholderEngine.Resolve("Date: %DATE% Year: %YEAR% User: %USERNAME%", t, overrides: null, now);

        result.Should().StartWith("Date: 2026-05-12 Year: 2026 User: ");
    }

    [Fact]
    public void Resolve_OverridesWinOverDefaults()
    {
        var t = MakeTemplate(new PlaceholderSpec("AUTHOR", "default-author", null));
        var result = PlaceholderEngine.Resolve("by %AUTHOR%", t, new Dictionary<string, string> { ["AUTHOR"] = "Ana" });
        result.Should().Be("by Ana");
    }

    [Fact]
    public void Resolve_LeavesUnknownPlaceholdersUnchanged()
    {
        var t = MakeTemplate();
        PlaceholderEngine.Resolve("Hello %NOPE%", t).Should().Be("Hello %NOPE%");
    }

    [Fact]
    public void Resolve_GuidIsSyntacticallyValid()
    {
        var t = MakeTemplate();
        var result = PlaceholderEngine.Resolve("%GUID%", t);
        Guid.TryParse(result, out _).Should().BeTrue();
    }

    [Fact]
    public void Resolve_DataTemplatesPassThroughUnchanged()
    {
        var t = MakeTemplate() with { TemplateType = ShellNewType.Data };
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        PlaceholderEngine.Resolve(bytes, t).Should().BeEquivalentTo(bytes);
    }
}
