using System.Security.Claims;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Enums.Organization;
using AuthApi.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

/// <summary>Regression tests for the independent review of the self organization profile.</summary>
public sealed class SelfOrganizationHardeningTests
{
    private const string ClientId = SelfOrganizationProfileTests.ClientId;

    [Fact]
    public async Task LockedOutOrUnverifiedAccount_IsRejected()
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var user = await s.F.Db.Users.SingleAsync(x => x.Id == s.UserId);
        user.LockoutEndUtc = new DateTime(2026, 11, 6, 5, 0, 0, DateTimeKind.Utc);
        await s.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, ClientId));

        user.LockoutEndUtc = null;
        user.IsAccountVerified = false;
        await s.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, ClientId));
    }

    [Fact]
    public async Task ReportingLine_RejectsARealAssignmentKeyBelongingToAnotherEmployee()
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var other = await s.F.CreateEmployeeAsync("NIP-901", "Pegawai Lain");
        await s.F.Placements.AssignDefinitiveAsync(other.Id,
            new CreateDefinitiveAssignmentCommand(s.StaffPositionId, true, s.F.Today, null, "Place"), other.Version, s.F.Actor);
        var foreignKey = (await s.F.Db.EmployeePositions.SingleAsync(x => x.EmployeeId == other.Id)).AssignmentKey;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => s.Self.GetReportingLineAsync(s.UserId, ClientId, foreignKey));
    }

    [Fact]
    public async Task SupervisorPositionChange_MovesNextEffectiveChange()
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var head = await s.F.Db.Positions.AsNoTracking().SingleAsync(x => x.Id == s.HeadPositionId);
        await s.Org.AddPositionVersionAsync(s.HeadPositionId,
            new PositionVersionCommand("Kepala Seksi Baru", PositionTrack.Line, true, 1, null, null, true, s.F.Today.AddDays(3), "Rename"),
            '"' + Convert.ToBase64String(head.RowVersion) + '"', s.F.Actor);

        var profile = await s.Self.GetAsync(s.UserId, ClientId);

        Assert.Equal(new DateTime(2026, 11, 8, 17, 0, 0, DateTimeKind.Utc), profile.NextEffectiveChangeAtUtc); // 2026-11-09 00:00 WIB
    }

    [Fact]
    public async Task ProfileVersion_IncludesAsOfAndIsDeterministicForTies()
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var extra1 = (await s.Org.CreatePositionAsync(new CreatePositionCommand(s.UnitId, "EXTRA1", "Extra 1", PositionTrack.Line, false, null,
            null, null, s.F.Today, "Create"), s.F.Actor)).Id;
        var extra2 = (await s.Org.CreatePositionAsync(new CreatePositionCommand(s.UnitId, "EXTRA2", "Extra 2", PositionTrack.Line, false, null,
            null, null, s.F.Today, "Create"), s.F.Actor)).Id;
        var version = s.F.Version(s.Employee.Id);
        var first = await s.F.Placements.AssignDefinitiveAsync(s.Employee.Id, new CreateDefinitiveAssignmentCommand(extra1, false, s.F.Today, null, "A"), version, s.F.Actor);
        await s.F.Placements.AssignDefinitiveAsync(s.Employee.Id, new CreateDefinitiveAssignmentCommand(extra2, false, s.F.Today, null, "B"), first.Employee.Version, s.F.Actor);

        var a = await s.Self.GetAsync(s.UserId, ClientId);
        var b = await s.Self.GetAsync(s.UserId, ClientId);

        Assert.Equal(a.ProfileVersion, b.ProfileVersion);
        Assert.Equal(a.Assignments.Select(x => x.Id), b.Assignments.Select(x => x.Id));
        Assert.Equal(a.Assignments.Skip(1).Select(x => x.Id).OrderBy(x => x), a.Assignments.Skip(1).Select(x => x.Id)); // ties sorted by AssignmentKey
    }

    [Fact]
    public async Task MissingMasterVersionAtAsOf_YieldsGenericMessageNotFrameworkText()
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var versions = s.F.Db.PositionVersions.Where(x => x.PositionId == s.StaffPositionId);
        s.F.Db.PositionVersions.RemoveRange(versions);
        await s.F.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => s.Self.GetAsync(s.UserId, ClientId));
        Assert.DoesNotContain("Sequence", ex.Message);
        Assert.Contains("Hubungi administrator", ex.Message);
    }

    private static SelfOrganizationController Controller(SelfOrganizationProfileService service, Guid? sub, string? aud, string? ifNoneMatch = null)
    {
        var http = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (sub is not null) claims.Add(new Claim("sub", sub.ToString()!));
        if (aud is not null) claims.Add(new Claim("aud", aud));
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        if (ifNoneMatch is not null) http.Request.Headers.IfNoneMatch = ifNoneMatch;
        return new SelfOrganizationController(service) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Controller_RejectsTokensMissingSubjectOrAudience(bool noSub, bool noAud)
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var controller = Controller(s.Self, noSub ? null : s.UserId, noAud ? null : ClientId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Get(default));
    }

    [Theory]
    [InlineData("exact")]
    [InlineData("weak")]
    [InlineData("star")]
    [InlineData("list")]
    public async Task Controller_AnswersNotModifiedForMatchingConditionalRequests(string form)
    {
        using var s = await SelfOrganizationProfileTests.Scenario.CreateAsync();
        var etag = "\"" + (await s.Self.GetAsync(s.UserId, ClientId)).ProfileVersion + "\"";
        var header = form switch { "exact" => etag, "weak" => "W/" + etag, "star" => "*", _ => "\"other\", " + etag };

        var result = (await Controller(s.Self, s.UserId, ClientId, header).Get(default)).Result;
        Assert.Equal(304, Assert.IsType<StatusCodeResult>(result).StatusCode);

        var stale = (await Controller(s.Self, s.UserId, ClientId, "\"stale\"").Get(default)).Result;
        Assert.IsType<OkObjectResult>(stale);
    }
}
