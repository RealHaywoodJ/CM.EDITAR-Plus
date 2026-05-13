using CM.EDITAR.Core;
using CM.EDITAR.FileCreator;
using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.FileCreator.Tests;

public class FileCreatorServiceTests : IDisposable
{
    private readonly string _root;
    private readonly TemplateService _templates;
    private readonly FileCreatorService _sut;

    public FileCreatorServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cmeditar-fc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _templates = new TemplateService(Path.Combine(_root, "tpl"));
        _sut = new FileCreatorService(_templates);
    }

    [Fact]
    public async Task Create_RejectsUnknownTemplateId()
    {
        var resp = await _sut.CreateAsync(new FileCreatorRequest
        {
            TemplateId = Guid.NewGuid(),
            TargetPath = Path.Combine(_root, "out.md"),
            Token = "n/a",
        });
        resp.Success.Should().BeFalse();
        resp.ErrorCode.Should().Be("UNKNOWN_TEMPLATE");
    }

    [Fact]
    public async Task Create_RefusesToOverwriteExistingTarget()
    {
        var meta = await _templates.CreateAsync(new TemplateMetadata
        {
            Id = Guid.Empty, Name = "t", Extensions = new[] { ".md" }, TemplateType = ShellNewType.FileName,
        }, System.Text.Encoding.UTF8.GetBytes("hi"));

        var target = Path.Combine(_root, "exists.md");
        await File.WriteAllTextAsync(target, "already here");

        var resp = await _sut.CreateAsync(new FileCreatorRequest
        {
            TemplateId = meta.Id, TargetPath = target, Token = "n/a",
        });

        resp.Success.Should().BeFalse();
        resp.ErrorCode.Should().Be("EXISTS");
    }

    [Fact]
    public async Task Create_WritesRenderedContentAtomically()
    {
        var meta = await _templates.CreateAsync(new TemplateMetadata
        {
            Id = Guid.Empty, Name = "t", Extensions = new[] { ".md" }, TemplateType = ShellNewType.FileName,
        }, System.Text.Encoding.UTF8.GetBytes("Hello %USERNAME%"));

        var target = Path.Combine(_root, "fresh.md");
        var resp = await _sut.CreateAsync(new FileCreatorRequest
        {
            TemplateId = meta.Id, TargetPath = target, Token = "n/a",
            PlaceholderOverrides = new Dictionary<string, string> { ["USERNAME"] = "Maya" },
        });

        resp.Success.Should().BeTrue();
        (await File.ReadAllTextAsync(target)).Should().Be("Hello Maya");
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }
}
