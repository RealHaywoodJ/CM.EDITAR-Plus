using CM.EDITAR.Core;
using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Templates.Tests;

public class CommandSanitizerTests
{
    private static TemplateMetadata Cmd(string source, bool approved) => new()
    {
        Id = Guid.NewGuid(),
        Name = "x",
        TemplateType = ShellNewType.Command,
        TemplateSource = source,
        CommandApproved = approved,
    };

    [Fact]
    public void StaticTemplates_AlwaysAllowed()
    {
        var t = new TemplateMetadata { Id = Guid.NewGuid(), Name = "x", TemplateType = ShellNewType.FileName };
        CommandSanitizer.Inspect(t).Allowed.Should().BeTrue();
    }

    [Fact]
    public void Command_RequiresExplicitApproval()
    {
        var r = CommandSanitizer.Inspect(Cmd("notepad.exe %1", approved: false));
        r.Allowed.Should().BeFalse();
        r.Reason.Should().Contain("approval");
    }

    [Fact]
    public void Command_RejectsNetworkPaths()
    {
        var r = CommandSanitizer.Inspect(Cmd(@"\\evil-host\share\bad.exe", approved: true));
        r.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Command_RejectsDownloadAndExecute()
    {
        var r = CommandSanitizer.Inspect(Cmd("powershell.exe -enc <stuff>", approved: true));
        r.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Command_AllowsBenignApprovedCommand()
    {
        var r = CommandSanitizer.Inspect(Cmd("notepad.exe %1", approved: true));
        r.Allowed.Should().BeTrue();
    }
}
