using CM.EDITAR.Core;
using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

/// <summary>
/// Verifies that <see cref="RegistryService.BuildManifestAsync"/> emits the right
/// payload value AND deletes the conflicting siblings so a type switch leaves no
/// stale ShellNew state behind.
/// </summary>
public class BuildManifestTypeSwitchTests
{
    [Fact]
    public async Task FileNameTarget_RemovesCommandAndDataValues()
    {
        var manifest = await new RegistryService().BuildManifestAsync(new[]
        {
            new StagedChange(".md", StagedAction.Edit, ShellNewType.FileName, FileName: "ShellNew\\Template.md"),
        });

        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "Command");
        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "Data");
        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.Modify && o.ValueName == "FileName");
    }

    [Fact]
    public async Task CommandTarget_RemovesFileNameAndDataValues()
    {
        var manifest = await new RegistryService().BuildManifestAsync(new[]
        {
            new StagedChange(".myx", StagedAction.Edit, ShellNewType.Command, Command: "myapp.exe %1"),
        });

        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "FileName");
        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "Data");
    }

    [Fact]
    public async Task DataTarget_EmitsBinaryValueAndRemovesOthers()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0xFF };
        var b64 = System.Convert.ToBase64String(payload);

        var manifest = await new RegistryService().BuildManifestAsync(new[]
        {
            new StagedChange(".rtf", StagedAction.Add, ShellNewType.Data, DataBase64: b64),
        });

        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.Modify
            && o.ValueName == "Data"
            && o.ValueKind == "REG_BINARY"
            && o.Value == "010203FF");
        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "FileName");
        manifest.Operations.Should().Contain(o =>
            o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == "Command");
    }

    [Fact]
    public async Task NullFileTarget_RemovesAllPayloadValues()
    {
        var manifest = await new RegistryService().BuildManifestAsync(new[]
        {
            new StagedChange(".txt", StagedAction.Edit, ShellNewType.NullFile),
        });

        foreach (var stale in new[] { "FileName", "Command", "Data" })
        {
            manifest.Operations.Should().Contain(o =>
                o.Kind == RegistryOperationKind.DeleteValue && o.ValueName == stale,
                $"NullFile target must clear stale {stale} value");
        }
    }
}
