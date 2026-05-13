using CM.EDITAR.Core;
using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

public class RegistryServiceTests
{
    [Fact]
    public async Task BuildManifestAsync_StagedAdd_ProducesNullFileAndItemNameOps()
    {
        var sut = new RegistryService();
        var manifest = await sut.BuildManifestAsync(new[]
        {
            new StagedChange(".md", StagedAction.Add, ShellNewType.NullFile, DisplayName: "Markdown File"),
        });

        manifest.Operations.Should().Contain(o =>
            o.ValueName == "NullFile" && o.Kind == RegistryOperationKind.Add);
        manifest.Operations.Should().Contain(o =>
            o.ValueName == "ItemName" && o.Value == "Markdown File");
        manifest.RequiresTypedConfirmation.Should().BeFalse();
        manifest.Operations.Should().OnlyContain(o => o.KeyPath.StartsWith("HKCU\\"));
    }

    [Fact]
    public async Task BuildManifestAsync_CommandTemplate_RequiresTypedConfirmation()
    {
        var sut = new RegistryService();
        var manifest = await sut.BuildManifestAsync(new[]
        {
            new StagedChange(".myx", StagedAction.Add, ShellNewType.Command, Command: "myapp.exe %1"),
        });
        manifest.RequiresTypedConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task BuildManifestAsync_DisableProducesDeleteOp()
    {
        var sut = new RegistryService();
        var manifest = await sut.BuildManifestAsync(new[]
        {
            new StagedChange(".md", StagedAction.Disable, ShellNewType.NullFile),
        });
        manifest.Operations.Should().ContainSingle(o => o.Kind == RegistryOperationKind.Delete);
    }

    [Fact]
    public async Task DiscoverAsync_NonWindows_ReturnsEmpty()
    {
        if (OperatingSystem.IsWindows()) return;
        var sut = new RegistryService();
        var result = await sut.DiscoverAsync();
        result.Should().BeEmpty();
    }
}
