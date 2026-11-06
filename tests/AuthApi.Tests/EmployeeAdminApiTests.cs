using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Enums.Employees;
using AuthApi.WebApi.Controllers;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeeQueryServiceTests
{
    private static IEmployeeQueryService Queries(EmployeeMutationFixture f) => new EmployeeQueryService(f.Db, f.Support);

    [Fact]
    public async Task Reads_RequireFreshSuperAdminMembership_NotJustAnActiveAccount()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var plain = new User { Username = "plain", Email = "plain@example.test", FullName = "Plain", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        f.Db.Users.Add(plain);
        await f.Db.SaveChangesAsync();
        var outsider = new EmployeeActor(plain.Id);
        var employee = await f.CreateEmployeeAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).ListAsync(null, 1, 20, outsider));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).GetAsync(employee.Id, outsider));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).GetHistoryAsync(employee.Id, outsider));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).FindEmployeeIdByUserAsync(plain.Id, outsider));
    }

    [Fact]
    public async Task Reads_RejectDeactivatedAdministratorEvenWithPriorSuperAdminRole()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        await f.CreateEmployeeAsync();
        var admin = f.Db.Users.Single(x => x.Id == f.Actor.UserId);
        admin.IsActive = false;
        await f.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Queries(f).ListAsync(null, 1, 20, f.Actor));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_RejectsOutOfRangePaging(int page, int pageSize)
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Queries(f).ListAsync(null, page, pageSize, f.Actor));
    }

    [Fact]
    public async Task List_PagesSearchesAndReportsStatusAndVersion()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        await f.CreateEmployeeAsync("NIP-001", "Alpha Person");
        await f.CreateEmployeeAsync("NIP-002", "Beta Person");
        await f.CreateEmployeeAsync("NIP-003", "Gamma Person");

        var page = await Queries(f).ListAsync(null, 2, 2, f.Actor);
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("NIP-003", page.Items[0].EmployeeNumber);
        Assert.Equal(EmployeeStatusCode.Active, page.Items[0].CurrentStatus);
        Assert.StartsWith("\"", page.Items[0].Version);

        var found = await Queries(f).ListAsync("Beta", 1, 20, f.Actor);
        Assert.Equal("NIP-002", Assert.Single(found.Items).EmployeeNumber);
    }

    [Fact]
    public async Task Get_ReturnsCurrentRecordsOnly_WhileHistoryKeepsSupersededRevisions()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var position = await f.AddPositionAsync("STAFF", null);
        var assigned = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(position, true, f.Today, null, "Initial"), employee.Version, f.Actor);
        await f.Placements.EndDefinitiveAsync(employee.Id,
            new EndDefinitiveAssignmentCommand(assigned.RecordId!.Value, f.Today.AddDays(10), "End"), assigned.Employee.Version, f.Actor);

        var detail = await Queries(f).GetAsync(employee.Id, f.Actor);
        var current = Assert.Single(detail.Placements);
        Assert.Equal("STAFF", current.PositionCode);
        Assert.Equal(f.Today.AddDays(10), current.EffectiveToExclusive);
        Assert.Equal(f.Version(employee.Id), detail.Employee.Version);

        var history = await Queries(f).GetHistoryAsync(employee.Id, f.Actor);
        Assert.Equal(2, history.Placements.Count);
        Assert.Contains(history.Placements, x => x.SupersededAtUtc != null);
    }

    [Fact]
    public async Task Get_And_History_ReturnNotFoundForUnknownEmployee()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Queries(f).GetAsync(Guid.NewGuid(), f.Actor));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Queries(f).GetHistoryAsync(Guid.NewGuid(), f.Actor));
    }
}

public sealed class EmployeeAdminHttpBoundaryTests
{
    [Theory]
    [InlineData(true, StatusCodes.Status428PreconditionRequired)]
    [InlineData(false, StatusCodes.Status412PreconditionFailed)]
    public async Task PreconditionFailures_MapToProblemDetailsWithExpectedStatus(bool missing, int expected)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, new EmployeePreconditionException(missing), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expected, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expected, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public void Filter_AcceptsAuthAdminBearerWithSubject() =>
        Run(Context(audience: "auth-admin", sub: Guid.NewGuid().ToString()));

    [Fact]
    public void Filter_RejectsApiKeyHeaderEvenWithValidBearer()
    {
        var context = Context(audience: "auth-admin", sub: Guid.NewGuid().ToString());
        context.HttpContext.Request.Headers["X-Api-Key"] = "anything";
        Assert.Throws<ForbiddenAccessException>(() => Run(context));
    }

    [Fact]
    public void Filter_RejectsTokenForAnotherApplication() =>
        Assert.Throws<ForbiddenAccessException>(() => Run(Context(audience: "erp", sub: Guid.NewGuid().ToString())));

    [Fact]
    public void Filter_RejectsAnonymousAndMissingSubject()
    {
        Assert.Throws<UnauthorizedAccessException>(() => Run(Context(audience: null, sub: null, authenticated: false)));
        Assert.Throws<UnauthorizedAccessException>(() => Run(Context(audience: "auth-admin", sub: "not-a-guid")));
    }

    [Fact]
    public void Controller_IsBearerOnlyAndRateLimited()
    {
        var type = typeof(EmployeesAdminController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(RequireAdminBearerAttribute)));
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute)));
        Assert.Null(Attribute.GetCustomAttribute(type, typeof(RequireAdminPermissionAttribute)));
    }

    [Fact]
    public void OrganizationController_IsBearerOnlyAndRateLimited()
    {
        var type = typeof(OrganizationAdminController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(RequireAdminBearerAttribute)));
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute)));
        Assert.Null(Attribute.GetCustomAttribute(type, typeof(RequireAdminPermissionAttribute)));
    }

    private static void Run(AuthorizationFilterContext context) => new AdminBearerOnlyFilter().OnAuthorization(context);

    private static AuthorizationFilterContext Context(string? audience, string? sub, bool authenticated = true)
    {
        var http = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (audience is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        if (sub is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
        http.User = new ClaimsPrincipal(authenticated ? new ClaimsIdentity(claims, "Bearer") : new ClaimsIdentity());
        return new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
    }
}

public sealed class EmployeeAdminRequestContractTests
{
    [Theory]
    [InlineData("""{"effectiveFrom":"2026-12-01","reason":"x"}""")]
    public void StatusRequest_MissingRequiredFieldIsDetectedNotDefaulted(string json)
    {
        var request = JsonSerializer.Deserialize<ChangeStatusRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void StatusRequest_WithAllFieldsIsValid()
    {
        var request = JsonSerializer.Deserialize<ChangeStatusRequest>(
            """{"statusCode":"RESIGNED","effectiveFrom":"2026-12-01","reason":"x"}""", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Empty(Validate(request));
        Assert.Equal(EmployeeStatusCode.Resigned, request.StatusCode);
    }

    [Fact]
    public void AssignmentAndActingRequests_RequireValueTypesExplicitly()
    {
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var assignment = JsonSerializer.Deserialize<CreateAssignmentRequest>(
            $$"""{"positionId":"{{Guid.NewGuid()}}","effectiveFrom":"2026-12-01","reason":"x"}""", web)!;
        Assert.NotEmpty(Validate(assignment));
        var acting = JsonSerializer.Deserialize<CreateActingRequest>(
            $$"""{"sourceAssignmentId":"{{Guid.NewGuid()}}","targetPositionId":"{{Guid.NewGuid()}}","effectiveFrom":"2026-12-01","decisionReference":"SK","reason":"x"}""", web)!;
        Assert.NotEmpty(Validate(acting));
    }

    [Fact]
    public async Task List_RejectsPageNumbersThatCouldOverflowSkip()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var queries = new EmployeeQueryService(f.Db, f.Support);
        await Assert.ThrowsAsync<ArgumentException>(() => queries.ListAsync(null, int.MaxValue, 100, f.Actor));
    }

    // MVC validates positional-record parameters by their attributes; emulate that per parameter.
    private static List<string> Validate(object request)
    {
        var errors = new List<string>();
        var ctor = request.GetType().GetConstructors().Single();
        foreach (var parameter in ctor.GetParameters())
        {
            var value = request.GetType().GetProperty(parameter.Name!)!.GetValue(request);
            foreach (var attribute in parameter.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute), true)
                .Cast<System.ComponentModel.DataAnnotations.ValidationAttribute>())
                if (!attribute.IsValid(value)) errors.Add(parameter.Name!);
        }
        return errors;
    }
}
