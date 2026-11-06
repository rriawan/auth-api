using System.Text.Json;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities;
using Xunit;

namespace AuthApi.Tests;

/// <summary>Contract used by the admin portal: string enums and the per-account organization lookup.</summary>
public sealed class EmployeePortalContractTests
{
    private static IEmployeeQueryService Queries(EmployeeMutationFixture f) => new EmployeeQueryService(f.Db, f.Support);

    [Fact]
    public async Task ResponseEnumsAreSerializedAsStrings()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();

        var detail = await Queries(f).GetAsync(employee.Id, f.Actor);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(detail, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal("Active", json.RootElement.GetProperty("employee").GetProperty("currentStatus").GetString());
        var status = json.RootElement.GetProperty("statuses")[0];
        Assert.Equal("Active", status.GetProperty("statusCode").GetString());
        Assert.Equal("Confirmed", status.GetProperty("recordState").GetString());
    }

    [Fact]
    public async Task GetByUser_ReturnsLinkedEmployeeOrNullForUnlinkedAccount()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var target = new User { Username = "t", Email = "t@example.test", FullName = "T", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        f.Db.Users.Add(target);
        await f.Db.SaveChangesAsync();
        var employee = await f.CreateEmployeeAsync();

        Assert.Null(await Queries(f).GetByUserAsync(target.Id, f.Actor));

        await f.Profiles.LinkUserAsync(employee.Id, target.Id, "Link", employee.Version, f.Actor);
        var linked = await Queries(f).GetByUserAsync(target.Id, f.Actor);
        Assert.Equal(employee.Id, linked!.Employee.Id);
        Assert.Equal(target.Id, linked.Employee.LinkedUserId);
    }

    [Fact]
    public async Task GetByUser_RequiresFreshSuperAdmin()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var plain = new User { Username = "p", Email = "p@example.test", FullName = "P", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        f.Db.Users.Add(plain);
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).GetByUserAsync(plain.Id, new EmployeeActor(plain.Id)));
    }
}
