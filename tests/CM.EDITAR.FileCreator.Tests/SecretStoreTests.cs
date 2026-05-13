using CM.EDITAR.FileCreator;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.FileCreator.Tests;

public class SecretStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly SecretStore _sut;

    public SecretStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cmeditar-secret-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _sut = new SecretStore(Path.Combine(_dir, "filecreator.secret"));
    }

    [Fact]
    public async Task GetOrCreateToken_IsStableAcrossCalls()
    {
        var first = await _sut.GetOrCreateTokenAsync();
        var second = await _sut.GetOrCreateTokenAsync();
        first.Should().Be(second);
        first.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public async Task Validate_AcceptsCorrectTokenAndRejectsOthers()
    {
        var token = await _sut.GetOrCreateTokenAsync();
        (await _sut.ValidateAsync(token)).Should().BeTrue();
        (await _sut.ValidateAsync("nope")).Should().BeFalse();
        (await _sut.ValidateAsync("")).Should().BeFalse();
    }

    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }
}
