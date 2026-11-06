using System.Text.Json;
using Xunit;

namespace AuthApi.Tests;

public sealed class OperationalTemplateCatalogTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static Catalog LoadCatalog(int version = 1)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "organization", $"operational-templates.v{version}.json");
        Assert.True(File.Exists(path), $"Template catalog was not copied to {path}");
        return JsonSerializer.Deserialize<Catalog>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Template catalog is empty.");
    }

    [Fact]
    public void Catalog_HasOnlyTheFiveApprovedDraftClassificationsAndExpectedCounts()
    {
        var catalog = LoadCatalog();

        Assert.Equal(1, catalog.SchemaVersion);
        Assert.Equal("DRAFT", catalog.State);
        Assert.Equal(new[] { "CAB-I", "CAB-II", "CAB-III", "HEAD_OFFICE", "PEMASAR" },
            catalog.Templates.Select(template => template.ClassificationCode).OrderBy(code => code, StringComparer.Ordinal));
        Assert.Equal(99, catalog.Templates.Sum(template => template.Units.Count));
        Assert.Equal(113, catalog.Templates.Sum(template => template.Positions.Count));
        Assert.Equal(new[] { 4, 3, 2, 89, 1 }, catalog.Templates
            .OrderBy(template => template.ClassificationCode, StringComparer.Ordinal)
            .Select(template => template.Units.Count));
        Assert.Equal(new[] { 8, 7, 6, 89, 3 }, catalog.Templates
            .OrderBy(template => template.ClassificationCode, StringComparer.Ordinal)
            .Select(template => template.Positions.Count));
    }

    [Fact]
    public void PositionLevels_AreTheFiveApprovedLevelsInOrder()
    {
        var catalog = LoadCatalog();
        Assert.Equal(new[] { "DIREKTUR", "KADIV", "KABAG", "KASI", "PELAKSANA" },
            catalog.Levels.OrderBy(level => level.DisplayOrder).Select(level => level.Code));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, catalog.Levels.OrderBy(level => level.DisplayOrder).Select(level => level.DisplayOrder));
        Assert.All(catalog.Levels, level => Assert.InRange(level.Name.Length, 1, 150));
    }

    [Fact]
    public void HeadOfficeLevels_FollowUnitTypeAndOperationalTemplatesStayUnmapped()
    {
        var catalog = LoadCatalog();
        var hq = GetTemplate(catalog, "HEAD_OFFICE");
        var expected = new Dictionary<string, string>
        {
            ["DIRECTORATE"] = "DIREKTUR", ["DIVISION"] = "KADIV", ["DEPARTMENT"] = "KABAG", ["SECTION"] = "KASI",
        };
        var unitTypes = hq.Units.ToDictionary(unit => unit.Code, unit => unit.Type);
        Assert.All(hq.Positions, position => Assert.Equal(expected[unitTypes[position.UnitCode]], position.LevelCode));
        Assert.All(catalog.Templates.Where(template => template.ClassificationCode != "HEAD_OFFICE")
            .SelectMany(template => template.Positions), position => Assert.Null(position.LevelCode));
    }

    [Fact]
    public void HeadOfficeTemplate_HasExpectedLevelsAndFollowsTheApprovedReportingLines()
    {
        var hq = GetTemplate(LoadCatalog(), "HEAD_OFFICE");

        Assert.Equal(new Dictionary<string, int>
        {
            ["DIRECTORATE"] = 3, ["DIVISION"] = 11, ["DEPARTMENT"] = 24, ["SECTION"] = 51,
        }, hq.Units.GroupBy(unit => unit.Type).ToDictionary(group => group.Key, group => group.Count()));

        // Every unit has exactly one head position (capacity 1) and nothing else.
        Assert.All(hq.Positions, position =>
        {
            Assert.True(position.IsUnitHead);
            Assert.Equal(1, position.MaxIncumbents);
            Assert.Equal("HEAD_" + position.UnitCode, position.Code);
        });

        // Directors have no supervisor; every other head reports to the head of its parent unit.
        var directorates = hq.Units.Where(unit => unit.Type == "DIRECTORATE").ToList();
        Assert.All(directorates, unit => Assert.Null(unit.ParentCode));
        Assert.All(hq.Units.Where(unit => unit.Type != "DIRECTORATE"), unit =>
            Assert.Equal("HEAD_" + unit.ParentCode, Position(hq, "HEAD_" + unit.Code).ReportsToCode));
        Assert.All(directorates, unit => Assert.Null(Position(hq, "HEAD_" + unit.Code).ReportsToCode));

        // Level hierarchy: Division under Directorate, Department under Division, Section under Department.
        var byCode = hq.Units.ToDictionary(unit => unit.Code);
        var expectedParentType = new Dictionary<string, string>
        {
            ["DIVISION"] = "DIRECTORATE", ["DEPARTMENT"] = "DIVISION", ["SECTION"] = "DEPARTMENT",
        };
        Assert.All(hq.Units.Where(unit => unit.Type != "DIRECTORATE"), unit =>
            Assert.Equal(expectedParentType[unit.Type], byCode[unit.ParentCode!].Type));

        // Spot checks against the source data.
        Assert.Equal("Aktuaris Perusahaan", Position(hq, "HEAD_DIV_02").Name);
        Assert.Equal("HEAD_DIR_02", Position(hq, "HEAD_DIV_02").ReportsToCode);
        Assert.Equal("Sekretaris Perusahaan", Position(hq, "HEAD_DIV_06").Name);
        Assert.Equal("HEAD_DIR_01", Position(hq, "HEAD_DIV_06").ReportsToCode);
        Assert.Equal("Kepala Unit Kerja Data Nasabah", Assert.Single(hq.Positions, p => p.Name == "Kepala Unit Kerja Data Nasabah").Name);
        Assert.Equal("Unit Kerja Data Nasabah", Assert.Single(hq.Units, u => u.Name == "Unit Kerja Data Nasabah").Name);
    }

    [Fact]
    public void Version2_KeepsVersion1StructureAndOnlyAddsLevelsAndHeadOfficeExecutors()
    {
        var v1 = LoadCatalog(1);
        var v2 = LoadCatalog(2);

        Assert.Equal(v1.Levels.Select(l => (l.Code, l.Name, l.DisplayOrder)), v2.Levels.Select(l => (l.Code, l.Name, l.DisplayOrder)));
        Assert.All(v2.Templates, template => Assert.Equal(2, template.Version));
        Assert.Equal(164, v2.Templates.Sum(template => template.Positions.Count));
        Assert.Equal(99, v2.Templates.Sum(template => template.Units.Count));

        foreach (var old in v1.Templates)
        {
            var next = GetTemplate(v2, old.ClassificationCode);
            Assert.Equal(old.Units.Select(u => (u.Code, u.ParentCode, u.Type, u.Name)), next.Units.Select(u => (u.Code, u.ParentCode, u.Type, u.Name)));
            foreach (var position in old.Positions)
            {
                var moved = Position(next, position.Code);
                Assert.Equal((position.UnitCode, position.ReportsToCode, position.Name, position.Track, position.IsUnitHead, position.MaxIncumbents),
                    (moved.UnitCode, moved.ReportsToCode, moved.Name, moved.Track, moved.IsUnitHead, moved.MaxIncumbents));
            }
        }
    }

    [Fact]
    public void Version2_MapsSectionHeadsAndExecutorsAndLeavesOfficeHeadsUnmapped()
    {
        var v2 = LoadCatalog(2);
        foreach (var template in v2.Templates.Where(t => t.ClassificationCode != "HEAD_OFFICE"))
        {
            Assert.All(template.Positions, position =>
            {
                var expected = position.Code == "HEAD" ? null
                    : position.Code.StartsWith("STAFF_", StringComparison.Ordinal) ? "PELAKSANA" : "KASI";
                Assert.Equal(expected, position.LevelCode);
            });
        }
    }

    [Fact]
    public void Version2_HeadOfficeHasOneMultiIncumbentExecutorPerSectionNamedAfterIt()
    {
        var hq = GetTemplate(LoadCatalog(2), "HEAD_OFFICE");
        var sections = hq.Units.Where(u => u.Type == "SECTION").ToList();
        Assert.Equal(51, sections.Count);
        foreach (var section in sections)
        {
            var executor = Position(hq, "STAFF_" + section.Code);
            var head = Position(hq, "HEAD_" + section.Code);
            Assert.Equal(section.Code, executor.UnitCode);
            Assert.Equal(head.Code, executor.ReportsToCode);
            Assert.False(executor.IsUnitHead);
            Assert.Null(executor.MaxIncumbents);
            Assert.Equal("PELAKSANA", executor.LevelCode);
            var suffix = section.Name.StartsWith("Seksi ", StringComparison.Ordinal) ? section.Name[6..] : section.Name;
            Assert.Equal("Pelaksana " + suffix, executor.Name);
        }
        Assert.Equal(51, hq.Positions.Count(p => p.Code.StartsWith("STAFF_", StringComparison.Ordinal)));
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
                Assert.Contains(unit.Type, new[] { "OTHER", "SECTION", "DIRECTORATE", "DIVISION", "DEPARTMENT" });
                if (unit.ParentCode is not null)
                    Assert.Contains(unit.ParentCode, units.Keys);
                AssertAcyclic(unit.Code, code => units[code].ParentCode);
            }

            var positions = template.Positions.ToDictionary(position => position.Code);
            foreach (var position in template.Positions)
            {
                Assert.Contains(position.UnitCode, units.Keys);
                Assert.Equal("LINE", position.Track);
                // Levels are mapped only for the head-office template; operational offices stay unmapped.
                if (template.ClassificationCode == "HEAD_OFFICE")
                    Assert.Contains(position.LevelCode, catalog.Levels.Select(level => level.Code));
                else
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
        public List<LevelEntry> Levels { get; init; } = [];
        public List<Template> Templates { get; init; } = [];
    }

    private sealed class LevelEntry
    {
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public int DisplayOrder { get; init; }
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
