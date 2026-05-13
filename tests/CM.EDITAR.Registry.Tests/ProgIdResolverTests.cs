using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

public class ProgIdResolverTests
{
    [Fact]
    public void Resolve_RequiresLeadingDot()
    {
        var sut = new ProgIdResolver();
        var act = () => sut.Resolve("md");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_OnNonWindows_ReturnsExtensionFallbackPath()
    {
        if (OperatingSystem.IsWindows()) return; // tested by integration on Windows

        var sut = new ProgIdResolver();
        var r = sut.Resolve(".cmeditarx");
        r.Extension.Should().Be(".cmeditarx");
        r.ProgId.Should().BeNull();
        r.FromUserChoice.Should().BeFalse();
        r.FromHkcuOverride.Should().BeFalse();
        r.ResolvedShellNewKeyPath.Should().Be(@"HKCU\Software\Classes\.cmeditarx\ShellNew");
    }
}
