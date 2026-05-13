using CM.EDITAR.Core;
using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Templates.Tests;

public class TemplateServiceTests : IDisposable
{
    private readonly string _root;
    private readonly TemplateService _sut;

    public TemplateServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cmeditar-tpl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _sut = new TemplateService(_root);
    }

    [Fact]
    public async Task Create_Then_Get_RoundTripsMetadata()
    {
        var meta = await _sut.CreateAsync(new TemplateMetadata
        {
            Id = Guid.Empty,
            Name = "Markdown",
            Extensions = new[] { ".md" },
            TemplateType = ShellNewType.FileName,
        }, body: System.Text.Encoding.UTF8.GetBytes("# %DATE%\n"));

        meta.Id.Should().NotBe(Guid.Empty);
        var fetched = await _sut.GetAsync(meta.Id);
        fetched!.Name.Should().Be("Markdown");
    }

    [Fact]
    public async Task Render_ResolvesPlaceholdersInBody()
    {
        var meta = await _sut.CreateAsync(new TemplateMetadata
        {
            Id = Guid.Empty, Name = "Doc",
            Extensions = new[] { ".md" }, TemplateType = ShellNewType.FileName,
        }, body: System.Text.Encoding.UTF8.GetBytes("Hello %USERNAME%"));

        var rendered = await _sut.RenderAsync(meta.Id, overrides: new Dictionary<string, string> { ["USERNAME"] = "Maya" });
        System.Text.Encoding.UTF8.GetString(rendered).Should().Be("Hello Maya");
    }

    [Fact]
    public async Task Create_RejectsUnapprovedCommandTemplates()
    {
        var act = () => _sut.CreateAsync(new TemplateMetadata
        {
            Id = Guid.Empty, Name = "bad", TemplateType = ShellNewType.Command,
            TemplateSource = "notepad.exe %1", CommandApproved = false,
        }, body: null);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }
}
