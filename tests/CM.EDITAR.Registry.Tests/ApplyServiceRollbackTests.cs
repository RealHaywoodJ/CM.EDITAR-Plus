using CM.EDITAR.ApplyService;
using CM.EDITAR.Core;
using CM.EDITAR.FileCreator;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

/// <summary>
/// ApplyService end-to-end tests against a fake <see cref="IRegistryService"/>. The Windows-only
/// branches that perform actual registry I/O are skipped; what we cover here is the contract that
/// surrounds them: snapshot is always created first, audit is appended on every outcome, and
/// the result reflects the cross-compile guard correctly on non-Windows hosts.
/// </summary>
public class ApplyServiceRollbackTests : IDisposable
{
    private readonly string _tempDir;

    public ApplyServiceRollbackTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cmeditar-rb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private (ApplyService.ApplyService sut, SnapshotService snaps, FakeRegistry fake, AuditLog audit) BuildSut(string slot)
    {
        var fake = new FakeRegistry();
        var snapshots = new SnapshotService(Path.Combine(_tempDir, "snaps-" + slot));
        var audit = new AuditLog(
            new SecretStore(Path.Combine(_tempDir, $"secret-{slot}")),
            Path.Combine(_tempDir, $"audit-{slot}.log"));
        var sut = new ApplyService.ApplyService(fake, snapshots, audit);
        return (sut, snapshots, fake, audit);
    }

    private static ApplyManifest BuildManifest() => new()
    {
        Operations = new List<RegistryOperation>
        {
            new()
            {
                Kind = RegistryOperationKind.Add,
                KeyPath = @"HKCU\Software\Classes\.x\ShellNew",
                ValueName = "NullFile",
                ValueKind = "REG_SZ",
                Value = "",
            },
        },
    };

    [Fact]
    public async Task ApplyAsync_AlwaysCreatesSnapshotBeforeAttemptingAnyWrite()
    {
        var (sut, snaps, _, _) = BuildSut("a");
        await sut.ApplyAsync(BuildManifest());
        var listed = await snaps.ListAsync();
        listed.Should().HaveCount(1);
        listed[0].Reason.Should().Be("pre-apply");
    }

    [Fact]
    public async Task ApplyAsync_OnNonWindowsHost_RefusesWritesAndReportsFailureWithSnapshotPath()
    {
        if (OperatingSystem.IsWindows()) return; // Windows path is exercised by integration on a Win runner.
        var (sut, _, _, _) = BuildSut("b");
        var result = await sut.ApplyAsync(BuildManifest());
        result.Success.Should().BeFalse();
        result.SnapshotPath.Should().NotBeNullOrEmpty();
        File.Exists(result.SnapshotPath!).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAsync_AppendsSignedAuditEntryEveryTime()
    {
        var (sut, _, _, audit) = BuildSut("c");
        await sut.ApplyAsync(BuildManifest());
        var entries = await audit.ReadAllAsync();
        entries.Should().NotBeEmpty();
        entries[^1].Operation.Should().Be("apply");
        entries[^1].Signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UndoLastAsync_NoSnapshots_ReturnsFailGracefully()
    {
        var (sut, _, _, _) = BuildSut("d");
        var r = await sut.UndoLastAsync();
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("No snapshots");
    }

    public void Dispose() { try { Directory.Delete(_tempDir, true); } catch { } }

    private sealed class FakeRegistry : IRegistryService
    {
        public bool IsSupported => true;
        public int NotifyCount;

        public Task<IReadOnlyList<ShellNewEntry>> DiscoverAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ShellNewEntry>>(Array.Empty<ShellNewEntry>());

        public Task<ProgIdResolution> ResolveProgIdAsync(string extension, CancellationToken ct = default) =>
            Task.FromResult(new ProgIdResolution(extension, null, false, false, $@"HKCU\Software\Classes\{extension}\ShellNew"));

        public Task<ApplyManifest> BuildManifestAsync(IEnumerable<StagedChange> staged, CancellationToken ct = default) =>
            Task.FromResult(new ApplyManifest());

        public void NotifyShellOfChange() => Interlocked.Increment(ref NotifyCount);

        public Task<bool> VerifyManifestAsync(ApplyManifest manifest, CancellationToken ct = default) =>
            Task.FromResult(true);
    }
}
