using System.Text.Json;
using CM.EDITAR.ApplyService;
using CM.EDITAR.Core;
using CM.EDITAR.FileCreator;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Registry.Tests;

public class AuditLogTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _logFile;
    private readonly string _secretFile;
    private readonly AuditLog _sut;

    public AuditLogTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cmeditar-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _logFile = Path.Combine(_tempDir, "changes.log");
        _secretFile = Path.Combine(_tempDir, "secret");
        _sut = new AuditLog(new SecretStore(_secretFile), _logFile);
    }

    [Fact]
    public async Task AppendAsync_WritesSignedJsonLine()
    {
        await _sut.AppendAsync(new AuditEntry
        {
            UserSid = "S-1-5-21-test",
            Operation = "apply",
            Success = true,
            ManifestId = Guid.NewGuid(),
            AffectedKeys = new[] { @"HKCU\Software\Classes\.md\ShellNew" },
            Message = "ok",
        });

        var lines = await File.ReadAllLinesAsync(_logFile);
        lines.Should().HaveCount(1);
        var parsed = JsonSerializer.Deserialize<AuditEntry>(lines[0]);
        parsed!.Signature.Should().NotBeNullOrEmpty();
        parsed.Signature.Should().HaveLength(64); // sha256 hex
    }

    [Fact]
    public async Task ReadAllAsync_RoundTripsAppendedEntries()
    {
        for (int i = 0; i < 3; i++)
            await _sut.AppendAsync(new AuditEntry { UserSid = "S", Operation = "apply", Success = true, Message = i.ToString() });
        var all = await _sut.ReadAllAsync();
        all.Should().HaveCount(3);
    }

    public void Dispose() { try { Directory.Delete(_tempDir, true); } catch { } }
}
