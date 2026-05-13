using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CM.EDITAR.Core;
using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Templates.Tests;

public class StarterPackImporterTests : IDisposable
{
    private readonly string _destDir;
    private readonly string _packDir;

    public StarterPackImporterTests()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cmeditar-sp-" + Guid.NewGuid().ToString("N"));
        _destDir = Path.Combine(tmp, "Templates");
        _packDir = Path.Combine(tmp, "pack");
        Directory.CreateDirectory(_destDir);
        Directory.CreateDirectory(_packDir);
    }

    // -------------------------------------------------------------------------
    // Basic import
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_EmptyDestination_CopiesAllTemplates()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id1, "Markdown Notebook", ".md", "# %DATE%\nBy %USERNAME%", builtInVersion: "1.0.0");
        CreateFakeTemplate(_packDir, id2, "Blank Text", ".txt", "Created: %DATE%\nAuthor: %USERNAME%", builtInVersion: "1.0.0");

        await StarterPackImporter.RunAsync(_destDir, _packDir);

        Directory.EnumerateDirectories(_destDir).Should().HaveCount(2);
        File.Exists(Path.Combine(_destDir, id1.ToString(), "metadata.json")).Should().BeTrue();
        File.Exists(Path.Combine(_destDir, id2.ToString(), "metadata.json")).Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_UserTemplate_IsNotTouched()
    {
        var userTemplateId = Guid.NewGuid();
        CreateFakeTemplate(_destDir, userTemplateId, "My Custom Template", ".txt", "my content", builtInVersion: null);

        var packId = Guid.NewGuid();
        CreateFakeTemplate(_packDir, packId, "Pack Template", ".md", "# %TITLE%", builtInVersion: "1.0.0");

        await StarterPackImporter.RunAsync(_destDir, _packDir);

        Directory.EnumerateDirectories(_destDir).Should().HaveCount(2,
            "pack template is imported but the pre-existing user template is preserved");
        Directory.Exists(Path.Combine(_destDir, userTemplateId.ToString())).Should().BeTrue();
        Directory.Exists(Path.Combine(_destDir, packId.ToString())).Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_MissingPackDir_DoesNotThrow()
    {
        var missingPack = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N"));
        var act = async () => await StarterPackImporter.RunAsync(_destDir, missingPack);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_StampsTimestamps()
    {
        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "Date Check", ".txt", "Created: %DATE%", builtInVersion: "1.0.0");

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await StarterPackImporter.RunAsync(_destDir, _packDir);
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var metaJson = await File.ReadAllTextAsync(Path.Combine(_destDir, id.ToString(), "metadata.json"));
        metaJson.Should().Contain("createdAt");

        using var doc = JsonDocument.Parse(metaJson);
        var root = doc.RootElement;
        var createdAt = root.GetProperty("createdAt").GetDateTimeOffset();
        createdAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task RunAsync_StoresBuiltInBodyHashOnFirstImport()
    {
        var id = Guid.NewGuid();
        const string body = "Hello %TITLE%";
        CreateFakeTemplate(_packDir, id, "Hash Test", ".txt", body, builtInVersion: "1.0.0");

        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var metaJson = await File.ReadAllTextAsync(Path.Combine(_destDir, id.ToString(), "metadata.json"));
        using var doc = JsonDocument.Parse(metaJson);
        doc.RootElement.TryGetProperty("builtInBodyHash", out var hashProp).Should().BeTrue();
        var storedHash = hashProp.GetString();
        storedHash.Should().NotBeNullOrEmpty();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        storedHash.Should().BeEquivalentTo(expectedHash, "hash should match SHA-256 of body");
    }

    [Fact]
    public async Task RunAsync_SkipsEntriesWithNonGuidNames()
    {
        Directory.CreateDirectory(Path.Combine(_packDir, "not-a-guid"));
        File.WriteAllText(Path.Combine(_packDir, "not-a-guid", "metadata.json"), "{}");

        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "Valid", ".txt", "hello", builtInVersion: "1.0.0");

        await StarterPackImporter.RunAsync(_destDir, _packDir);

        Directory.Exists(Path.Combine(_destDir, "not-a-guid")).Should().BeFalse();
        Directory.Exists(Path.Combine(_destDir, id.ToString())).Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Selective update — version comparison
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_PackHasNewerVersion_UnEditedTemplate_Overwrites()
    {
        var id = Guid.NewGuid();
        const string oldBody = "old body";
        const string newBody = "new body from pack";
        // First import at v1.0.0 (sets timestamps and body hash)
        CreateFakeTemplate(_packDir, id, "Old Name", ".txt", oldBody, builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // Upgrade: ship v1.1.0 with a new body
        CreateFakeTemplate(_packDir, id, "New Name", ".txt", newBody, builtInVersion: "1.1.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var meta = await ReadInstalledMetaAsync(id);
        meta.Name.Should().Be("New Name");
        var installedBody = await File.ReadAllTextAsync(
            Path.Combine(_destDir, id.ToString(), "template.txt"));
        installedBody.Should().Be(newBody);
    }

    [Fact]
    public async Task RunAsync_PackHasNewerVersion_UserEditedBodyOnDisk_IsPreserved()
    {
        var id = Guid.NewGuid();
        // First import at v1.0.0
        CreateFakeTemplate(_packDir, id, "Pack Name", ".txt", "original body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // User directly edits the body file on disk (no UI → timestamps unchanged)
        var installedBodyPath = Path.Combine(_destDir, id.ToString(), "template.txt");
        await File.WriteAllTextAsync(installedBodyPath, "my custom body edited on disk");

        // Upgrade: ship v1.1.0
        CreateFakeTemplate(_packDir, id, "Pack Name", ".txt", "updated pack body", builtInVersion: "1.1.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var bodyOnDisk = await File.ReadAllTextAsync(installedBodyPath);
        bodyOnDisk.Should().Be("my custom body edited on disk",
            "direct on-disk edits must be preserved even when timestamps are unchanged");
    }

    [Fact]
    public async Task RunAsync_PackHasNewerVersion_UserEditedViaUI_IsPreserved()
    {
        var id = Guid.NewGuid();
        // First import at v1.0.0
        CreateFakeTemplate(_packDir, id, "Pack Name", ".txt", "original body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // Simulate UI edit: bump modifiedAt forward (as UpdateAsync would)
        var metaPath = Path.Combine(_destDir, id.ToString(), "metadata.json");
        var meta = await ReadInstalledMetaAsync(id);
        var editedMeta = meta with { Name = "User-renamed", ModifiedAt = meta.CreatedAt.AddHours(1) };
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(editedMeta));

        // Upgrade: ship v1.1.0
        CreateFakeTemplate(_packDir, id, "Pack Name", ".txt", "updated pack body", builtInVersion: "1.1.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var afterMeta = await ReadInstalledMetaAsync(id);
        afterMeta.Name.Should().Be("User-renamed", "UI edits (bumped modifiedAt) must be preserved");
    }

    [Fact]
    public async Task RunAsync_PackHasSameVersion_DoesNotOverwrite()
    {
        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "Pack v1", ".txt", "pack body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // Re-run with same version but different name — should not overwrite
        CreateFakeTemplate(_packDir, id, "Pack v1 renamed", ".txt", "different body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var meta = await ReadInstalledMetaAsync(id);
        meta.Name.Should().Be("Pack v1");
    }

    [Fact]
    public async Task RunAsync_PackHasOlderVersion_DoesNotOverwrite()
    {
        var id = Guid.NewGuid();
        // Install v2 manually
        CreateFakeTemplate(_packDir, id, "Installed v2", ".txt", "v2 body", builtInVersion: "2.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // Pack goes back to v1 — must not overwrite
        CreateFakeTemplate(_packDir, id, "Pack v1", ".txt", "v1 body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var meta = await ReadInstalledMetaAsync(id);
        meta.Name.Should().Be("Installed v2");
    }

    // -------------------------------------------------------------------------
    // RestoreTemplateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RestoreTemplateAsync_ExistingPackTemplate_OverwritesEdits()
    {
        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "Original Pack Name", ".txt", "original body", builtInVersion: "1.0.0");
        CreateFakeTemplate(_destDir, id, "User-edited Name", ".txt", "user content", builtInVersion: "1.0.0");

        var result = await StarterPackImporter.RestoreTemplateAsync(id, _destDir, _packDir);

        result.Should().BeTrue();
        var meta = await ReadInstalledMetaAsync(id);
        meta.Name.Should().Be("Original Pack Name");
    }

    [Fact]
    public async Task RestoreTemplateAsync_IdNotInPack_ReturnsFalse()
    {
        var missingId = Guid.NewGuid();
        var result = await StarterPackImporter.RestoreTemplateAsync(missingId, _destDir, _packDir);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreTemplateAsync_StampsTimestampsAndBodyHash()
    {
        var id = Guid.NewGuid();
        const string body = "pack body content";
        CreateFakeTemplate(_packDir, id, "Pack", ".txt", body, builtInVersion: "1.0.0");
        CreateFakeTemplate(_destDir, id, "Edited", ".txt", "edited body", builtInVersion: "1.0.0",
            createdAt: "2026-01-01T00:00:00+00:00", modifiedAt: "2026-02-01T00:00:00+00:00");

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await StarterPackImporter.RestoreTemplateAsync(id, _destDir, _packDir);
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var metaJson = await File.ReadAllTextAsync(Path.Combine(_destDir, id.ToString(), "metadata.json"));
        using var doc = JsonDocument.Parse(metaJson);
        var root = doc.RootElement;

        var createdAt = root.GetProperty("createdAt").GetDateTimeOffset();
        createdAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        root.GetProperty("builtInBodyHash").GetString()
            .Should().BeEquivalentTo(expectedHash);
    }

    // -------------------------------------------------------------------------
    // HasUserDriftAsync — body hash check
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HasUserDriftAsync_BodyUnchanged_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        const string body = "unchanged body";
        CreateFakeTemplate(_packDir, id, "T", ".txt", body, builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var installedDir = Path.Combine(_destDir, id.ToString());
        var meta = await ReadInstalledMetaAsync(id);
        var drift = await StarterPackImporter.HasUserDriftAsync(installedDir, meta);
        drift.Should().BeFalse();
    }

    [Fact]
    public async Task HasUserDriftAsync_BodyEditedOnDisk_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "T", ".txt", "original body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        // Direct on-disk edit — no timestamps changed
        await File.WriteAllTextAsync(Path.Combine(_destDir, id.ToString(), "template.txt"), "edited by user");

        var installedDir = Path.Combine(_destDir, id.ToString());
        var meta = await ReadInstalledMetaAsync(id);
        var drift = await StarterPackImporter.HasUserDriftAsync(installedDir, meta);
        drift.Should().BeTrue("body hash mismatch must be detected as drift");
    }

    [Fact]
    public async Task HasUserDriftAsync_MetadataEditedViaUI_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        CreateFakeTemplate(_packDir, id, "T", ".txt", "body", builtInVersion: "1.0.0");
        await StarterPackImporter.RunAsync(_destDir, _packDir);

        var metaPath = Path.Combine(_destDir, id.ToString(), "metadata.json");
        var meta = await ReadInstalledMetaAsync(id);
        var editedMeta = meta with { ModifiedAt = meta.CreatedAt.AddHours(2) };
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(editedMeta));

        var installedDir = Path.Combine(_destDir, id.ToString());
        var drift = await StarterPackImporter.HasUserDriftAsync(installedDir, editedMeta);
        drift.Should().BeTrue("bumped modifiedAt must be detected as metadata drift");
    }

    [Fact]
    public async Task HasUserDriftAsync_LegacyInstallWithoutBodyHash_FallsBackToTimestamp()
    {
        var id = Guid.NewGuid();
        // Create an installed template that lacks builtInBodyHash (legacy)
        CreateFakeTemplate(_destDir, id, "Legacy", ".txt", "body", builtInVersion: "1.0.0",
            createdAt: "2026-01-01T00:00:00+00:00", modifiedAt: "2026-01-01T00:00:00+00:00");
        // No builtInBodyHash in metadata → legacy install
        var metaPath = Path.Combine(_destDir, id.ToString(), "metadata.json");
        var meta = JsonSerializer.Deserialize<TemplateMetadata>(await File.ReadAllTextAsync(metaPath))!;
        meta.BuiltInBodyHash.Should().BeNull("this test depends on no hash being stored");

        var drift = await StarterPackImporter.HasUserDriftAsync(
            Path.Combine(_destDir, id.ToString()), meta);
        drift.Should().BeFalse("equal timestamps mean no drift in legacy mode");
    }

    // -------------------------------------------------------------------------
    // IsNewerVersion unit tests
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("1.1.0", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.1.0", false)]
    [InlineData("2.0.0", null, true)]
    [InlineData(null, "1.0.0", false)]
    [InlineData(null, null, false)]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.2.3-rc.1", "1.2.2", true)]
    public void IsNewerVersion_ReturnsExpected(string? pack, string? installed, bool expected)
    {
        StarterPackImporter.IsNewerVersion(pack, installed).Should().Be(expected);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void CreateFakeTemplate(
        string root,
        Guid id,
        string name,
        string ext,
        string body,
        string? builtInVersion,
        string? createdAt = null,
        string? modifiedAt = null)
    {
        var dir = Path.Combine(root, id.ToString());
        Directory.CreateDirectory(dir);

        var now = "2026-01-01T00:00:00+00:00";
        var builtInVersionJson = builtInVersion is null
            ? ""
            : $"""
,
  "builtInVersion": "{builtInVersion}"
""";

        var meta = $$"""
            {
              "id": "{{id}}",
              "name": "{{name}}",
              "extensions": [ "{{ext}}" ],
              "category": "Starter Pack",
              "defaultFilename": "New File",
              "templateType": 1,
              "templateSource": null,
              "dataBase64": null,
              "placeholders": [],
              "createdAt": "{{createdAt ?? now}}",
              "modifiedAt": "{{modifiedAt ?? now}}",
              "author": "CM.EDITAR+",
              "commandApproved": false{{builtInVersionJson}}
            }
            """;
        File.WriteAllText(Path.Combine(dir, "metadata.json"), meta);
        File.WriteAllText(Path.Combine(dir, $"template{ext}"), body);
    }

    private async Task<TemplateMetadata> ReadInstalledMetaAsync(Guid id)
    {
        var path = Path.Combine(_destDir, id.ToString(), "metadata.json");
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<TemplateMetadata>(json)!;
    }

    public void Dispose()
    {
        try { Directory.Delete(Path.GetDirectoryName(_destDir)!, recursive: true); } catch { }
    }
}
