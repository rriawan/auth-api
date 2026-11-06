using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Organization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class OrganizationTemplateApplyTests
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
            foreach (var code in new[] { "CAB-I", "CAB-II" })
                if (!await f.Db.OfficeClassifications.AnyAsync(x => x.Code == code))
                    f.Db.OfficeClassifications.Add(new OfficeClassification { Code = code, DisplayName = code });
            f.Db.PositionLevels.Add(new PositionLevel { Code = "KASI", Name = "Kepala Seksi", DisplayOrder = 4 });
            await f.Db.SaveChangesAsync();
            return new Harness(f);
        }

        public Task<OrganizationMutationResult> Office(string code, string classification = "CAB-II") =>
            Writes.CreateOfficeAsync(new CreateOfficeCommand(code, "Kantor " + code, classification, Today, "Create"), Actor);

        /// <summary>Root unit + child section; head of root, head of section reporting to it, and a multi-incumbent executor.</summary>
        public async Task<StructureTemplate> Template(string classification = "CAB-II", int version = 1, bool published = false, bool cyclic = false)
        {
            var levelId = (await F.Db.PositionLevels.SingleAsync(x => x.Code == "KASI")).Id;
            var t = new StructureTemplate { ClassificationCode = classification, Version = version };
            if (published) { t.State = StructureTemplateState.Published; t.PublishedAtUtc = DateTime.UtcNow; t.PublishedByUserId = Actor.UserId; }
            var root = new TemplateOrgUnit { TemplateId = t.Id, UnitCode = "ROOT", UnitType = OrgUnitType.Other, Name = "Kantor" };
            var sec = new TemplateOrgUnit { TemplateId = t.Id, UnitCode = "SEK", UnitType = OrgUnitType.Section, Name = "Seksi", ParentTemplateOrgUnitId = root.Id };
            if (cyclic) { root.ParentTemplateOrgUnitId = sec.Id; }
            var head = new TemplatePosition { TemplateId = t.Id, TemplateOrgUnitId = root.Id, PositionCode = "HEAD", Name = "Kepala", IsUnitHead = true, MaxIncumbents = 1, Track = PositionTrack.Line };
            var sh = new TemplatePosition { TemplateId = t.Id, TemplateOrgUnitId = sec.Id, PositionCode = "SEK_HEAD", Name = "Kepala Seksi", IsUnitHead = true, MaxIncumbents = 1, Track = PositionTrack.Line, ReportsToTemplatePositionId = head.Id, LevelId = levelId };
            var staff = new TemplatePosition { TemplateId = t.Id, TemplateOrgUnitId = sec.Id, PositionCode = "STAFF", Name = "Pelaksana", Track = PositionTrack.Line, ReportsToTemplatePositionId = sh.Id };
            F.Db.StructureTemplates.Add(t);
            F.Db.TemplateOrgUnits.AddRange(root, sec);
            await F.Db.SaveChangesAsync();
            F.Db.TemplatePositions.AddRange(head, sh, staff);
            await F.Db.SaveChangesAsync();
            return t;
        }

        public ApplyTemplateCommand Apply(Guid templateId, string key = "k1", string reason = "Pilot") =>
            new(templateId, Today, reason, "SK-1", key);
    }

    [Fact]
    public async Task Publish_RequiresDraftAndIfMatch_AndAppliesOnlyAfterwards()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office("910");
        var t = await h.Template();

        await Assert.ThrowsAsync<EmployeePreconditionException>(() => h.Writes.PublishTemplateAsync(t.Id, new PublishTemplateCommand("SK"), null, h.Actor));
        var etag = EmployeeMutationSupport.ETag(t.RowVersion);
        var preview = await h.Reads.PreviewTemplateApplicationAsync(office.Id, t.Id, h.Today, h.Actor);
        Assert.False(preview.CanApply);
        Assert.Contains(preview.Blockers, x => x.Contains("dipublikasikan"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id), h.Actor));

        var published = await h.Writes.PublishTemplateAsync(t.Id, new PublishTemplateCommand("SK publish"), etag, h.Actor);
        Assert.NotEqual(etag, published.Version);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.PublishTemplateAsync(t.Id, new PublishTemplateCommand("again"), published.Version, h.Actor));
        Assert.True((await h.Reads.PreviewTemplateApplicationAsync(office.Id, t.Id, h.Today, h.Actor)).CanApply);
        Assert.Contains(h.F.Db.AuditLogs, x => x.EventType == AuthApi.Domain.Enums.AuditEventType.OrganizationTemplatePublished);
    }

    [Fact]
    public async Task Apply_CreatesUnitsPositionsAndLevels_ThenReplaysIdempotently()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var office = await h.Office("911");
        var t = await h.Template(published: true);

        var result = await h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id), h.Actor);
        Assert.False(result.Replayed);
        Assert.Equal(2, result.UnitsCreated);
        Assert.Equal(3, result.PositionsCreated);

        var structure = await h.Reads.GetStructureAsync(office.Id, h.Today, h.Actor);
        var root = Assert.Single(structure.Units);
        Assert.Equal("ROOT", root.UnitCode);
        var child = Assert.Single(root.Children);
        Assert.Equal(2, child.Positions.Count);
        var head = Assert.Single(root.Positions);
        Assert.Equal(head.Id, child.Positions.Single(x => x.PositionCode == "SEK_HEAD").ReportsToPositionId);
        Assert.NotNull(await h.F.Db.PositionVersions.Where(v => v.LevelId != null).Select(v => v.LevelId).FirstOrDefaultAsync());
        Assert.Null(child.Positions.Single(x => x.PositionCode == "STAFF").MaxIncumbents);

        var replay = await h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id), h.Actor);
        Assert.True(replay.Replayed);
        Assert.Equal(result.ApplicationId, replay.ApplicationId);
        Assert.Equal(2, await h.F.Db.OrgUnits.CountAsync(x => x.OfficeId == office.Id));
        Assert.Single(h.F.Db.OfficeTemplateApplications.Where(x => x.OfficeId == office.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id, reason: "Lain"), h.Actor));
    }

    [Fact]
    public async Task Apply_RejectsNonEmptyOfficeWrongClassificationAndUnknownTargets()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var t = await h.Template(published: true);
        var wrong = await h.Office("912", "CAB-I");
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.ApplyTemplateAsync(wrong.Id, h.Apply(t.Id), h.Actor));

        var office = await h.Office("913");
        await h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id), h.Actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.ApplyTemplateAsync(office.Id, h.Apply(t.Id, "k2"), h.Actor));

        var preview = await h.Reads.PreviewTemplateApplicationAsync(office.Id, t.Id, h.Today, h.Actor);
        Assert.False(preview.CanApply);
        Assert.Contains(preview.Blockers, x => x.Contains("kosong"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => h.Writes.ApplyTemplateAsync(Guid.NewGuid(), h.Apply(t.Id, "k3"), h.Actor));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => h.Writes.ApplyTemplateAsync(office.Id, h.Apply(Guid.NewGuid(), "k4"), h.Actor));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Writes.ApplyTemplateAsync(office.Id, new ApplyTemplateCommand(t.Id, h.Today.AddDays(-1), "x", null, "k5"), h.Actor));
    }

    [Fact]
    public async Task Publish_RejectsInvalidContent_AndPreviewFlagsBackdatedDate()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var badCode = await h.Template(version: 3);
        (await h.F.Db.TemplatePositions.FirstAsync(x => x.TemplateId == badCode.Id && x.PositionCode == "STAFF")).PositionCode = "bad code";
        await h.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.PublishTemplateAsync(badCode.Id, new PublishTemplateCommand("x"),
            EmployeeMutationSupport.ETag(badCode.RowVersion), h.Actor));

        var loop = await h.Template(version: 4);
        var staff = await h.F.Db.TemplatePositions.FirstAsync(x => x.TemplateId == loop.Id && x.PositionCode == "STAFF");
        var sekHead = await h.F.Db.TemplatePositions.FirstAsync(x => x.TemplateId == loop.Id && x.PositionCode == "SEK_HEAD");
        sekHead.ReportsToTemplatePositionId = staff.Id;
        await h.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.PublishTemplateAsync(loop.Id, new PublishTemplateCommand("x"),
            EmployeeMutationSupport.ETag(loop.RowVersion), h.Actor));

        var office = await h.Office("915");
        var ok = await h.Template(version: 5, published: true);
        var past = await h.Reads.PreviewTemplateApplicationAsync(office.Id, ok.Id, h.Today.AddDays(-1), h.Actor);
        Assert.False(past.CanApply);
        Assert.Contains(past.Blockers, x => x.Contains("masa lalu"));
    }

    [Fact]
    public async Task Publish_RejectsCyclicTemplate_AndNonAdminIsForbidden()
    {
        var h = await Harness.CreateAsync(); using var _ = h.F;
        var cyclic = await h.Template(version: 2, cyclic: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Writes.PublishTemplateAsync(cyclic.Id, new PublishTemplateCommand("x"),
            EmployeeMutationSupport.ETag(cyclic.RowVersion), h.Actor));

        var stranger = new EmployeeActor(Guid.NewGuid(), null, null);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => h.Reads.ListTemplatesAsync(null, stranger));
        var office = await h.Office("914");
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => h.Writes.ApplyTemplateAsync(office.Id, h.Apply(cyclic.Id), stranger));
        var list = await h.Reads.ListTemplatesAsync("CAB-II", h.Actor);
        Assert.Equal(3, Assert.Single(list).PositionCount);
    }
}
