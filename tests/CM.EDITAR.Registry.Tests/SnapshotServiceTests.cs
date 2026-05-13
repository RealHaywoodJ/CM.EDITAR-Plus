using CM.EDITAR.Registry;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

public class SnapshotServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SnapshotService _sut;

    public SnapshotServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cmeditar-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _sut = new SnapshotService(_tempDir);
    }

    [Fact]
    public async Task CreateAsync_WritesRegFileAndSidecarMetadata()
    {
        var meta = await _sut.CreateAsync(new[] { @"HKCU\Software\Classes\.md\ShellNew" }, "test", manifestId: null);

        File.Exists(meta.RegFilePath).Should().BeTrue();
        File.Exists(Path.ChangeExtension(meta.RegFilePath, ".json")).Should().BeTrue();
        meta.Sha256.Should().HaveLength(64);
        meta.ExportedKeys.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_NeverOverwritesExistingSnapshot()
    {
        var first = await _sut.CreateAsync(new[] { @"HKCU\Software\Classes\.md\ShellNew" }, "test", null);
        var second = await _sut.CreateAsync(new[] { @"HKCU\Software\Classes\.md\ShellNew" }, "test", null);
        first.RegFilePath.Should().NotBe(second.RegFilePath);
    }

    [Fact]
    public async Task ListAsync_ReturnsNewestFirst()
    {
        await _sut.CreateAsync(new[] { @"HKCU\Software\Classes\.a" }, "test", null);
        await Task.Delay(50);
        await _sut.CreateAsync(new[] { @"HKCU\Software\Classes\.b" }, "test", null);
        var all = await _sut.ListAsync();
        all.Should().HaveCount(2);
        all[0].CreatedAt.Should().BeOnOrAfter(all[1].CreatedAt);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
