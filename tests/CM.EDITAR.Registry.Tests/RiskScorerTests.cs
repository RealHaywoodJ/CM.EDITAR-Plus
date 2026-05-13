using CM.EDITAR.Core;
using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

public class RiskScorerTests
{
    [Theory]
    [InlineData(".txt", ShellNewType.NullFile, RiskLevel.Recommended)]
    [InlineData(".md", ShellNewType.FileName, RiskLevel.Recommended)]
    [InlineData(".docx", ShellNewType.FileName, RiskLevel.Recommended)]
    [InlineData(".bat", ShellNewType.NullFile, RiskLevel.High)]
    [InlineData(".exe", ShellNewType.FileName, RiskLevel.High)]
    [InlineData(".py", ShellNewType.NullFile, RiskLevel.Warning)]
    public void Score_ClassifiesKnownExtensions(string ext, ShellNewType type, RiskLevel expected)
    {
        RiskScorer.Score(ext, type, command: null).Should().Be(expected);
    }

    [Fact]
    public void Score_CommandTypeIsAlwaysHigh()
    {
        RiskScorer.Score(".txt", ShellNewType.Command, "notepad.exe %1").Should().Be(RiskLevel.High);
    }
}
