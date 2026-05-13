using CM.EDITAR.Core;
using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

/// <summary>
/// Verification + rollback behaviors. The actual registry I/O is Windows-only, but the
/// non-Windows codepath of <see cref="RegistryService.VerifyManifestAsync"/> still has a
/// well-defined contract (returns false because there is no registry to verify against),
/// which guards the rollback branch in <c>ApplyService</c>.
/// </summary>
public class RegistryServiceVerifyTests
{
    [Fact]
    public async Task VerifyManifestAsync_NonWindows_ReturnsFalse_TriggeringRollback()
    {
        if (OperatingSystem.IsWindows()) return;

        var sut = new RegistryService();
        var manifest = new ApplyManifest
        {
            Operations = new List<RegistryOperation>
            {
                new()
                {
                    Kind = RegistryOperationKind.Add,
                    KeyPath = @"HKCU\Software\Classes\.test\ShellNew",
                    ValueName = "NullFile",
                    ValueKind = "REG_SZ",
                    Value = "",
                },
            },
        };

        // The contract under non-Windows hosts is "cannot verify" → false → caller should rollback.
        var verified = await sut.VerifyManifestAsync(manifest);
        verified.Should().BeFalse();
    }

    [Fact]
    public async Task BuildManifest_OnUserChoiceExtension_StillTargetsHkcuShellNew()
    {
        // Even when UserChoice is set, manifest construction must remain HKCU-only and
        // never schedule overwrites of UserChoice itself. We assert the keypath prefix
        // and that no operation touches FileExts\<ext>\UserChoice.
        var sut = new RegistryService();
        var manifest = await sut.BuildManifestAsync(new[]
        {
            new StagedChange(".md", StagedAction.Add, ShellNewType.NullFile, DisplayName: "Markdown"),
        });

        manifest.Operations.Should().OnlyContain(o => o.KeyPath.StartsWith("HKCU\\"));
        manifest.Operations.Should().NotContain(o =>
            o.KeyPath.Contains(@"\FileExts\", StringComparison.OrdinalIgnoreCase) &&
            o.KeyPath.EndsWith(@"\UserChoice", StringComparison.OrdinalIgnoreCase));
    }
}
