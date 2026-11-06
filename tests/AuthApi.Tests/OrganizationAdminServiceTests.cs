using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Organization;
using AuthApi.Domain.Rules.Organization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class OrganizationAdminServiceTests
{
    private sealed class Harness(EmployeeMutationFixture f)
    {
        public EmployeeMutationFixture F { get; } = f;
        public OrganizationAdminService Writes { get; } = new(f.Db, f.Runner, f.Support, f.Placements);
        public OrganizationQueryService Reads { get; } = new(f.Db, f.Support);
        public EmployeeActor Actor => F.Actor;
        public DateOnly Today => F.Today;

        public static async Task<Harness> CreateAsync()
        {
            var f = await EmployeeMutationFixture.CreateAsync();
            f.Db.OfficeClassifications.Add(new OfficeClassification { Code = "CAB-I", DisplayName = "Cabang I" });
            await f.Db.SaveChangesAsync();
            return new Harness(f);
        }

        public Task<OrganizationMutationResult> Office(string code = "901") =>
            Writes.CreateOfficeAsync(new CreateOfficeCommand(code, "Test Office " + code, "CAB-I", Today, "Create"), Actor);

        public Task<OrganizationMutationResult> Unit(Guid officeId, string code, Guid? parent = null) =>
            Writes.CreateUnitAsync(new CreateUnitCommand(officeId, code, parent, OrgUnitType.Section, "Unit " + code, Today, "Create"), Actor);

        public Task<OrganizationMutationResult> Position(Guid unitId, string code, bool head = false, int? capacity = null, Guid? reportsTo = null) =>
            Writes.CreatePositionAsync(new CreatePositionCommand(unitId, code, "Position " + code, PositionTrack.Line, head,
                head ? 1 : capacity, null, reportsTo, Today, "Create"), Actor);
    }

    [Fact]
    public async Task CreatesOfficeUnitAndPosition_AndStructureShowsVacancy()
    {
        using var f = (await Harness.CreateAsync()).F; var h = new Harness(f);
        var office = await h.Office();
        var root = await h.Unit(office.Id, "ROOT");
        var child = await h.Unit(office.Id, "SEKSI", root.Id);
        var head = await h.Position(root.Id, "KEPALA", head: true);
        await h.Position(child.Id, "STAFF", reportsTo: head.Id);

        var tree = await h.Reads.GetStructureAsync(office.Id, null, h.Actor);

        var rootNode = Assert.Single(tree.Units);
        Assert.Equal("ROOT", rootNode.UnitCode);
        Assert.True(Assert.Single(rootNode.Positions).IsVacant);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal(head.Id, Assert.Single(childNode.Positions).ReportsToPositionId);
        Assert.StartsWith("\"", office.Version);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("1234")]
    [InlineData("ABC")]
    public async Task RejectsMalformedOfficeCode(string code)
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        await Assert.ThrowsAsync<ArgumentException>(() => h.Office(code));
    }

    [Fact]
    public async Task RejectsDuplicateOfficeCodeUnknownClassificationAndBackdating()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        await h.Office("902");
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Office("902"));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Writes.CreateOfficeAsync(
            new CreateOfficeCommand("903", "X", "NOPE", h.Today, "Create"), h.Actor));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Writes.CreateOfficeAsync(
            new CreateOfficeCommand("904", "X", "CAB-I", h.Today.AddDays(-1), "Create"), h.Actor));
    }

    [Fact]
    public async Task OfficeVersion_RequiresFreshEtag_RotatesIt_AndKeepsHistory()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office();
        var command = new OfficeVersionCommand("Renamed", "CAB-I", true, h.Today.AddDays(10), "Rename");

        var missing = await Assert.ThrowsAsync<EmployeePreconditionException>(() => h.Writes.AddOfficeVersionAsync(office.Id, command, null, h.Actor));
        Assert.True(missing.IsMissing);
        var changed = await h.Writes.AddOfficeVersionAsync(office.Id, command, office.Version, h.Actor);
        Assert.NotEqual(office.Version, changed.Version);
        var stale = await Assert.ThrowsAsync<EmployeePreconditionException>(() => h.Writes.AddOfficeVersionAsync(office.Id,
            command with { EffectiveFrom = h.Today.AddDays(20) }, office.Version, h.Actor));
        Assert.False(stale.IsMissing);

        var current = await h.Reads.GetOfficeAsync(office.Id, false, h.Actor);
        Assert.Equal(2, current.Versions.Count);
        Assert.Equal(h.Today.AddDays(10), current.Versions[0].EffectiveToExclusive);
        var history = await h.Reads.GetOfficeAsync(office.Id, true, h.Actor);
        Assert.True(history.Versions.Count > current.Versions.Count);
    }

    [Fact]
    public async Task EffectiveDateTooFarInTheFutureIsRejected_AndTruncatedRevisionKeepsItsOwnReason()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        await Assert.ThrowsAsync<ArgumentException>(() => h.Writes.CreateOfficeAsync(
            new CreateOfficeCommand("912", "X", "CAB-I", h.Today.AddYears(40), "Typo year"), h.Actor));

        var office = await h.Office("913");
        await h.Writes.AddOfficeVersionAsync(office.Id,
            new OfficeVersionCommand("Renamed", "CAB-I", true, h.Today.AddDays(10), "Rename later", ""), office.Version, h.Actor);
        var history = await h.Reads.GetOfficeAsync(office.Id, true, h.Actor);
        var truncated = Assert.Single(history.Versions, v => v.SupersededAtUtc == null && v.EffectiveToExclusive == h.Today.AddDays(10));
        Assert.Equal("Create", truncated.ChangeReason);
        Assert.Null(Assert.Single(history.Versions, v => v.Name == "Renamed").DecisionReference);
    }

    [Fact]
    public async Task ListPositions_ForUnknownScopeIsNotFound()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => h.Reads.ListPositionsAsync(Guid.NewGuid(), null, null, h.Actor));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => h.Reads.ListPositionsAsync(null, Guid.NewGuid(), null, h.Actor));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Reads.ListPositionsAsync(null, null, null, h.Actor));
    }

    [Fact]
    public async Task UnitHierarchyCycleOverFutureDatesIsRejected()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office();
        var a = await h.Unit(office.Id, "A");
        var b = await h.Unit(office.Id, "B", a.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.AddUnitVersionAsync(a.Id,
            new UnitVersionCommand(b.Id, OrgUnitType.Section, "A", true, h.Today.AddDays(1), "Make cycle"), a.Version, h.Actor));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParentFromAnotherOfficeIsRejected()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var first = await h.Office("905");
        var second = await h.Office("906");
        var foreign = await h.Unit(first.Id, "ROOT");
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Unit(second.Id, "CHILD", foreign.Id));
    }

    [Fact]
    public async Task OnlyOneActiveHeadPerUnit_AndHeadMustHaveCapacityOne()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office();
        var unit = await h.Unit(office.Id, "ROOT");
        await h.Position(unit.Id, "HEAD1", head: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Position(unit.Id, "HEAD2", head: true));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Writes.CreatePositionAsync(new CreatePositionCommand(unit.Id, "HEAD3", "x",
            PositionTrack.Line, true, 2, null, null, h.Today, "Bad capacity"), h.Actor));
    }

    [Fact]
    public async Task PositionCannotReportToItselfOrToAnotherOffice()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office("907");
        var other = await h.Office("908");
        var unit = await h.Unit(office.Id, "ROOT");
        var otherUnit = await h.Unit(other.Id, "ROOT");
        var foreign = await h.Position(otherUnit.Id, "BOSS");
        var mine = await h.Position(unit.Id, "STAFF");

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Position(unit.Id, "X", reportsTo: foreign.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.AddPositionVersionAsync(mine.Id,
            new PositionVersionCommand("STAFF", PositionTrack.Line, false, null, null, mine.Id, true, h.Today.AddDays(1), "Self"),
            mine.Version, h.Actor));
    }

    [Fact]
    public async Task DeactivatingAPositionThatHoldsAnIncumbentIsRejected()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office();
        var unit = await h.Unit(office.Id, "ROOT");
        var position = await h.Position(unit.Id, "STAFF");
        var employee = await h.F.CreateEmployeeAsync();
        await h.F.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(position.Id, true, h.Today, null, "Place"), employee.Version, h.Actor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.AddPositionVersionAsync(position.Id,
            new PositionVersionCommand("STAFF", PositionTrack.Line, false, null, null, null, false, h.Today.AddDays(5), "Archive"),
            position.Version, h.Actor));
    }

    [Fact]
    public async Task ArchivingAnOfficeWithActiveUnitsIsRejected()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office();
        await h.Unit(office.Id, "ROOT");
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.AddOfficeVersionAsync(office.Id,
            new OfficeVersionCommand("Test", "CAB-I", false, h.Today.AddDays(3), "Archive"), office.Version, h.Actor));
    }

    [Fact]
    public async Task WritesAndReadsRequireFreshSuperAdmin()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var plain = new User { Username = "plain2", Email = "plain2@example.test", FullName = "Plain", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        h.F.Db.Users.Add(plain);
        await h.F.Db.SaveChangesAsync();
        var outsider = new EmployeeActor(plain.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => h.Writes.CreateOfficeAsync(
            new CreateOfficeCommand("909", "X", "CAB-I", h.Today, "Create"), outsider));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => h.Reads.ListOfficesAsync(null, null, null, 1, 20, outsider));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => h.Reads.ListClassificationsAsync(outsider));
    }

    [Fact]
    public async Task ListOffices_FiltersPagesAndRejectsBadPaging()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        await h.Office("910");
        await h.Office("911");

        var page = await h.Reads.ListOfficesAsync("911", "CAB-I", null, 1, 20, h.Actor);
        var only = Assert.Single(page.Items);
        Assert.Equal("911", only.OfficeCode);
        Assert.Equal(true, only.IsActive);
        await Assert.ThrowsAsync<ArgumentException>(() => h.Reads.ListOfficesAsync(null, null, null, 0, 20, h.Actor));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Reads.ListOfficesAsync(null, null, null, 1, 101, h.Actor));
    }
}

public sealed class OrganizationIntegrityRulesTests
{
    private static readonly DateOnly D = new(2026, 1, 1);

    [Fact]
    public void RejectsGapsAndOverlapsInOneUnitTimeline()
    {
        var office = Guid.NewGuid(); var unit = Guid.NewGuid();
        OfficeVersion O(DateOnly from, DateOnly? to) => new() { OfficeId = office, EffectiveFrom = from, EffectiveToExclusive = to };
        OrgUnitVersion U(DateOnly from, DateOnly? to) => new() { OrgUnitId = unit, EffectiveFrom = from, EffectiveToExclusive = to, Name = "u" };

        var gap = new OfficeSnapshot([O(D, null)], [U(D, D.AddDays(5)), U(D.AddDays(7), null)], new Dictionary<Guid, Guid>(), []);
        Assert.Contains("gaps", Assert.Throws<InvalidOperationException>(() => OrganizationIntegrityRules.Validate(gap)).Message);
        var overlap = new OfficeSnapshot([O(D, null)], [U(D, null), U(D.AddDays(3), null)], new Dictionary<Guid, Guid>(), []);
        Assert.Contains("overlap", Assert.Throws<InvalidOperationException>(() => OrganizationIntegrityRules.Validate(overlap)).Message);
    }

    [Fact]
    public void AcceptsAContiguousValidStructureAndIgnoresSupersededRows()
    {
        var office = Guid.NewGuid(); var unit = Guid.NewGuid(); var position = Guid.NewGuid();
        var snapshot = new OfficeSnapshot(
            [new OfficeVersion { OfficeId = office, EffectiveFrom = D }],
            [new OrgUnitVersion { OrgUnitId = unit, EffectiveFrom = D, Name = "u" },
             new OrgUnitVersion { OrgUnitId = unit, EffectiveFrom = D, Name = "stale", IsActive = false, SupersededAtUtc = DateTime.UtcNow }],
            new Dictionary<Guid, Guid> { [position] = unit },
            [new PositionVersion { PositionId = position, EffectiveFrom = D, Name = "p" }]);
        OrganizationIntegrityRules.Validate(snapshot);
    }

    [Fact]
    public void ActivePositionNeedsItsUnitActiveForTheWholePeriod()
    {
        var office = Guid.NewGuid(); var unit = Guid.NewGuid(); var position = Guid.NewGuid();
        var snapshot = new OfficeSnapshot(
            [new OfficeVersion { OfficeId = office, EffectiveFrom = D }],
            [new OrgUnitVersion { OrgUnitId = unit, EffectiveFrom = D, EffectiveToExclusive = D.AddDays(10), Name = "u" },
             new OrgUnitVersion { OrgUnitId = unit, EffectiveFrom = D.AddDays(10), IsActive = false, Name = "u" }],
            new Dictionary<Guid, Guid> { [position] = unit },
            [new PositionVersion { PositionId = position, EffectiveFrom = D, Name = "p" }]);
        Assert.Throws<InvalidOperationException>(() => OrganizationIntegrityRules.Validate(snapshot));
    }
}
