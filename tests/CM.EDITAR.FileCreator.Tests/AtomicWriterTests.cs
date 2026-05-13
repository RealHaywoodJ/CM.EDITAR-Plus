using CM.EDITAR.FileCreator;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.FileCreator.Tests;

public class AtomicWriterTests : IDisposable
{
    private readonly string _dir;

    public AtomicWriterTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cmeditar-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Write_CreatesFileAndLeavesNoTempBehind()
    {
        var target = Path.Combine(_dir, "out.txt");
        await AtomicWriter.WriteAsync(target, System.Text.Encoding.UTF8.GetBytes("hello"), overwrite: false);

        File.Exists(target).Should().BeTrue();
        await File.ReadAllTextAsync(target).ConfigureAwait(false);
        Directory.GetFiles(_dir, ".cmeditar~*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task Write_RefusesOverwriteByDefault()
    {
        var target = Path.Combine(_dir, "out.txt");
        await File.WriteAllTextAsync(target, "first");
        var act = () => AtomicWriter.WriteAsync(target, new byte[] { 1, 2, 3 }, overwrite: false);
        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task Write_AllowsOverwriteWhenRequested()
    {
        var target = Path.Combine(_dir, "out.txt");
        await File.WriteAllTextAsync(target, "first");
        await AtomicWriter.WriteAsync(target, System.Text.Encoding.UTF8.GetBytes("second"), overwrite: true);
        (await File.ReadAllTextAsync(target)).Should().Be("second");
    }

    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }
}
