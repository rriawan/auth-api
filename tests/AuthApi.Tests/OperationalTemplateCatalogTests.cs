using System.Text.Json;
using Xunit;

namespace AuthApi.Tests;

public sealed class OperationalTemplateCatalogTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static Catalog LoadCatalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "organization", "operational-templates.v1.json");
        Assert.True(File.Exists(path), $"Template catalog was not copied to {path}");
        return JsonSerializer.Deserialize<Catalog>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Template catalog is empty.");
    }

    [Fact]
    public void Catalog_HasOnlyTheFourApprovedDraftClassificationsAndExpectedCounts()
    {
        var catalog = LoadCatalog();

        Assert.Equal(1, catalog.SchemaVersion);
        Assert.Equal("DRAFT", catalog.State);
        Assert.Equal(new[] { "CAB-I", "CAB-II", "CAB-III", "PEMASAR" },
            catalog.Templates.Select(template => template.ClassificationCode).OrderBy(code => code));
        Assert.Equal(10, catalog.Templates.Sum(template => template.Units.Count));
        Assert.Equal(24, catalog.Templates.Sum(template => template.Positions.Count));
        Assert.Equal(new[] { 4, 3, 2, 1 }, catalog.Templates
            .OrderBy(template => template.ClassificationCode)
            .Select(template => template.Units.Count));
        Assert.Equal(new[] { 8, 7, 6, 3 }, catalog.Templates
            .OrderBy(template => template.ClassificationCode)
            .Select(template => template.Positions.Count));
    }

    [Fact]
    public void ReportingLines_FollowTheApprovedOperationalStructures()
    {
        var catalog = LoadCatalog();
        var cab1 = GetTemplate(catalog, "CAB-I");
        var cab2 = GetTemplate(catalog, "CAB-II");
        var cab3 = GetTemplate(catalog, "CAB-III");
        var pemasar = GetTemplate(catalog, "PEMASAR");

        Assert.Equal("HEAD_TECHNICAL", Position(cab1, "STAFF_UNDERWRITING").ReportsToCode);
        Assert.Equal("HEAD_TECHNICAL", Position(cab1, "STAFF_CLAIMS_SUBROGATION").ReportsToCode);
        Assert.Equal("HEAD_OPERATIONS", Position(cab2, "STAFF_FINANCE").ReportsToCode);
        Assert.Equal("HEAD", Position(cab3, "STAFF_MARKETING").ReportsToCode);
        Assert.Equal("ROOT", Position(cab3, "STAFF_MARKETING").UnitCode);
        Assert.Equal("HEAD", Position(pemasar, "STAFF_ADMIN").ReportsToCode);
        Assert.Equal("HEAD", Position(pemasar, "STAFF_FINANCE").ReportsToCode);
    }

    [Fact]
    public void Templates_HaveValidAcyclicGraphsAndApprovedCapacityWithoutAuthorizationData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "organization", "operational-templates.v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        AssertNoAuthorizationFields(document.RootElement);

        var catalog = LoadCatalog();
        foreach (var template in catalog.Templates)
        {
            Assert.Equal(1, template.Version);
            Assert.Equal(template.Units.Count, template.Units.Select(unit => unit.Code).Distinct().Count());
            Assert.Equal(template.Positions.Count, template.Positions.Select(position => position.Code).Distinct().Count());

            var units = template.Units.ToDictionary(unit => unit.Code);
            foreach (var unit in template.Units)
            {
                Assert.Contains(unit.Type, new[] { "OTHER", "SECTION" });
                if (unit.ParentCode is not null)
                    Assert.Contains(unit.ParentCode, units.Keys);
                AssertAcyclic(unit.Code, code => units[code].ParentCode);
            }

            var positions = template.Positions.ToDictionary(position => position.Code);
            foreach (var position in template.Positions)
            {
                Assert.Contains(position.UnitCode, units.Keys);
                Assert.Equal("LINE", position.Track);
                Assert.Null(position.LevelCode);
                if (position.ReportsToCode is not null)
                    Assert.Contains(position.ReportsToCode, positions.Keys);

                if (position.IsUnitHead)
                    Assert.Equal(1, position.MaxIncumbents);
                else
                    Assert.Null(position.MaxIncumbents);

                AssertAcyclic(position.Code, code => positions[code].ReportsToCode);
            }
        }
    }

    private static Template GetTemplate(Catalog catalog, string classificationCode) =>
        Assert.Single(catalog.Templates, template => template.ClassificationCode == classificationCode);

    private static PositionEntry Position(Template template, string code) =>
        Assert.Single(template.Positions, position => position.Code == code);

    private static void AssertAcyclic(string start, Func<string, string?> parentOf)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = start;
        while (current is not null)
        {
            Assert.True(visited.Add(current), $"Cycle detected from node {start}.");
            current = parentOf(current)!;
        }
    }

    private static void AssertNoAuthorizationFields(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                Assert.DoesNotContain(property.Name, new[] { "role", "roles", "permission", "permissions", "authorization" }, StringComparer.OrdinalIgnoreCase);
                AssertNoAuthorizationFields(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                AssertNoAuthorizationFields(item);
        }
    }

    private sealed class Catalog
    {
        public int SchemaVersion { get; init; }
        public string State { get; init; } = "";
        public List<Template> Templates { get; init; } = [];
    }

    private sealed class Template
    {
        public string ClassificationCode { get; init; } = "";
        public int Version { get; init; }
        public List<Unit> Units { get; init; } = [];
        public List<PositionEntry> Positions { get; init; } = [];
    }

    private sealed class Unit
    {
        public string Code { get; init; } = "";
        public string? ParentCode { get; init; }
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
    }

    private sealed class PositionEntry
    {
        public string Code { get; init; } = "";
        public string UnitCode { get; init; } = "";
        public string? ReportsToCode { get; init; }
        public string Name { get; init; } = "";
        public string Track { get; init; } = "";
        public bool IsUnitHead { get; init; }
        public int? MaxIncumbents { get; init; }
        public string? LevelCode { get; init; }
    }
}
