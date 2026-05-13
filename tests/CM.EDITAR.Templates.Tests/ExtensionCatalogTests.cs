using CM.EDITAR.Templates;
using FluentAssertions;
using Xunit;

namespace CM.EDITAR.Templates.Tests;

/// <summary>
/// Invariants for the comprehensive ShellNew extension catalog. These tests
/// guard the same rules the Node generator script enforces at build time.
/// </summary>
public class ExtensionCatalogTests
{
    [Fact]
    public void Catalog_HasAtLeast300UniqueExtensions()
    {
        var unique = ExtensionCatalog.All
            .Select(e => e.Ext)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        unique.Should().BeGreaterOrEqualTo(300,
            "the v1.3.0 publish target requires a 300+ unique-extension catalog");
    }

    [Fact]
    public void EveryEntry_BelongsToAWiredCategory()
    {
        var wired = new HashSet<string>(ExtensionCatalog.Categories);
        var stragglers = ExtensionCatalog.All
            .Where(e => !wired.Contains(e.Category))
            .Select(e => $"{e.Category}::{e.Ext}")
            .ToArray();

        stragglers.Should().BeEmpty("every entry must map to one of the 11 wired sidebar categories");
    }

    [Fact]
    public void EveryWiredCategory_HasAtLeastOneEntry()
    {
        var present = ExtensionCatalog.All
            .Select(e => e.Category)
            .ToHashSet();

        foreach (var cat in ExtensionCatalog.Categories)
            present.Should().Contain(cat, $"category '{cat}' must have at least one entry");
    }

    [Fact]
    public void NoDuplicateExtensions_WithinTheSameCategory()
    {
        var dupes = ExtensionCatalog.All
            .GroupBy(e => (e.Category, e.Ext), StringComparer.OrdinalIgnoreCase.Wrap())
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Category}::{g.Key.Ext}")
            .ToArray();

        dupes.Should().BeEmpty("cross-listing across categories is allowed, but never within one");
    }

    [Fact]
    public void EveryHighRiskEntry_IsDisabledByDefault()
    {
        var leaks = ExtensionCatalog.All
            .Where(e => e.Risk == "high" && e.State != "disabled")
            .Select(e => $"{e.Category}::{e.Ext}")
            .ToArray();

        leaks.Should().BeEmpty("high-risk entries (executes on double-click) must ship disabled");
    }

    [Fact]
    public void ObviousExecutables_AreClassifiedHigh()
    {
        var mustBeHigh = new[] { ".exe", ".bat", ".cmd", ".com", ".ps1", ".vbs", ".wsf", ".crx", ".msi" };
        foreach (var ext in mustBeHigh)
        {
            var entry = ExtensionCatalog.All.FirstOrDefault(e =>
                string.Equals(e.Ext, ext, StringComparison.OrdinalIgnoreCase));
            entry.Should().NotBeNull($"{ext} is part of the v1.3.0 risk-policy guard list");
            entry!.Risk.Should().Be("high", $"{ext} executes on double-click");
            entry.State.Should().Be("disabled", $"{ext} must be off by default");
        }
    }
}

/// <summary>
/// Tiny helper so we can use the case-insensitive comparer with tuple keys
/// in the GroupBy invariant test.
/// </summary>
internal static class ComparerExtensions
{
    public static IEqualityComparer<(string Category, string Ext)> Wrap(this StringComparer inner) =>
        new TupleComparer(inner);

    private sealed class TupleComparer : IEqualityComparer<(string Category, string Ext)>
    {
        private readonly StringComparer _inner;
        public TupleComparer(StringComparer inner) => _inner = inner;

        public bool Equals((string Category, string Ext) x, (string Category, string Ext) y) =>
            _inner.Equals(x.Category, y.Category) && _inner.Equals(x.Ext, y.Ext);

        public int GetHashCode((string Category, string Ext) obj) =>
            HashCode.Combine(_inner.GetHashCode(obj.Category), _inner.GetHashCode(obj.Ext));
    }
}
