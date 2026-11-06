using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using AuthApi.Domain.Enums.Organization;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

public sealed record CreateOfficeRequest(
    [Required, MaxLength(3)] string OfficeCode,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(20)] string ClassificationCode,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record OfficeVersionRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(20)] string ClassificationCode,
    [Required] bool? IsActive,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record CreateUnitRequest(
    [Required] Guid OfficeId,
    [Required, MaxLength(50)] string UnitCode,
    Guid? ParentOrgUnitId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] OrgUnitType? UnitType,
    [Required, MaxLength(200)] string Name,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record UnitVersionRequest(
    Guid? ParentOrgUnitId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] OrgUnitType? UnitType,
    [Required, MaxLength(200)] string Name,
    [Required] bool? IsActive,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record CreatePositionRequest(
    [Required] Guid OrgUnitId,
    [Required, MaxLength(50)] string PositionCode,
    [Required, MaxLength(200)] string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] PositionTrack? Track,
    [Required] bool? IsUnitHead,
    int? MaxIncumbents,
    Guid? LevelId,
    Guid? ReportsToPositionId,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record PositionVersionRequest(
    [Required, MaxLength(200)] string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] PositionTrack? Track,
    [Required] bool? IsUnitHead,
    int? MaxIncumbents,
    Guid? LevelId,
    Guid? ReportsToPositionId,
    [Required] bool? IsActive,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

/// <summary>
/// Organization master administration (planning §8.2). Bearer 'auth-admin' only; administrator rights are re-checked against
/// the database inside every service call (D-06: SuperAdmin only in the first release). Position levels and structure
/// templates are intentionally not exposed yet (D-05 is only partially decided).
/// </summary>
[ApiController]
[Route("api/v1/admin/organization")]
[RequireAdminBearer]
[EnableRateLimiting(RateLimitPolicies.AdminEmployees)]
public sealed class OrganizationAdminController(IOrganizationAdminService writes, IOrganizationQueryService reads) : ControllerBase
{
    // ---- reads --------------------------------------------------------------------------------------------

    [HttpGet("classifications")]
    public async Task<ActionResult<IReadOnlyList<ClassificationDto>>> Classifications(CancellationToken ct)
    {
        NoStore();
        return Ok(await reads.ListClassificationsAsync(Actor(), ct));
    }

    [HttpGet("offices")]
    public async Task<ActionResult<OfficePageDto>> Offices([FromQuery] string? search, [FromQuery] string? classification,
        [FromQuery] DateOnly? asOf, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        NoStore();
        return Ok(await reads.ListOfficesAsync(search, classification, asOf, page, pageSize, Actor(), ct));
    }

    [HttpGet("offices/{id:guid}", Name = nameof(GetOffice))]
    public async Task<ActionResult<OfficeDetailDto>> GetOffice(Guid id, [FromQuery] bool includeHistory = false, CancellationToken ct = default)
    {
        NoStore();
        var detail = await reads.GetOfficeAsync(id, includeHistory, Actor(), ct);
        Response.Headers.ETag = detail.Version;
        return Ok(detail);
    }

    [HttpGet("offices/{id:guid}/structure")]
    public async Task<ActionResult<OfficeStructureDto>> Structure(Guid id, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        NoStore();
        return Ok(await reads.GetStructureAsync(id, asOf, Actor(), ct));
    }

    [HttpGet("units")]
    public async Task<ActionResult<IReadOnlyList<UnitSummaryDto>>> Units([FromQuery, Required] Guid? officeId, [FromQuery] DateOnly? asOf,
        CancellationToken ct)
    {
        NoStore();
        return Ok(await reads.ListUnitsAsync(officeId!.Value, asOf, Actor(), ct));
    }

    [HttpGet("units/{id:guid}", Name = nameof(GetUnit))]
    public async Task<ActionResult<UnitDetailDto>> GetUnit(Guid id, [FromQuery] bool includeHistory = false, CancellationToken ct = default)
    {
        NoStore();
        var detail = await reads.GetUnitAsync(id, includeHistory, Actor(), ct);
        Response.Headers.ETag = detail.Version;
        return Ok(detail);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<IReadOnlyList<PositionSummaryDto>>> Positions([FromQuery] Guid? officeId, [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        NoStore();
        return Ok(await reads.ListPositionsAsync(officeId, unitId, asOf, Actor(), ct));
    }

    [HttpGet("positions/{id:guid}", Name = nameof(GetPosition))]
    public async Task<ActionResult<PositionDetailDto>> GetPosition(Guid id, [FromQuery] bool includeHistory = false, CancellationToken ct = default)
    {
        NoStore();
        var detail = await reads.GetPositionAsync(id, includeHistory, Actor(), ct);
        Response.Headers.ETag = detail.Version;
        return Ok(detail);
    }

    // ---- offices ------------------------------------------------------------------------------------------

    [HttpPost("offices")]
    public async Task<IActionResult> CreateOffice([FromBody] CreateOfficeRequest r, CancellationToken ct) =>
        Created(await writes.CreateOfficeAsync(new CreateOfficeCommand(r.OfficeCode, r.Name, r.ClassificationCode, r.EffectiveFrom,
            r.Reason, r.DecisionReference), Actor(), ct), nameof(GetOffice));

    [HttpPost("offices/{id:guid}/versions")]
    public async Task<IActionResult> AddOfficeVersion(Guid id, [FromBody] OfficeVersionRequest r, CancellationToken ct) =>
        Mutated(await writes.AddOfficeVersionAsync(id, new OfficeVersionCommand(r.Name, r.ClassificationCode, r.IsActive!.Value,
            r.EffectiveFrom, r.Reason, r.DecisionReference), IfMatch(), Actor(), ct));

    // ---- units --------------------------------------------------------------------------------------------

    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest r, CancellationToken ct) =>
        Created(await writes.CreateUnitAsync(new CreateUnitCommand(r.OfficeId, r.UnitCode, r.ParentOrgUnitId, r.UnitType!.Value, r.Name,
            r.EffectiveFrom, r.Reason, r.DecisionReference), Actor(), ct), nameof(GetUnit));

    [HttpPost("units/{id:guid}/versions")]
    public async Task<IActionResult> AddUnitVersion(Guid id, [FromBody] UnitVersionRequest r, CancellationToken ct) =>
        Mutated(await writes.AddUnitVersionAsync(id, new UnitVersionCommand(r.ParentOrgUnitId, r.UnitType!.Value, r.Name, r.IsActive!.Value,
            r.EffectiveFrom, r.Reason, r.DecisionReference), IfMatch(), Actor(), ct));

    // ---- positions ----------------------------------------------------------------------------------------

    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest r, CancellationToken ct) =>
        Created(await writes.CreatePositionAsync(new CreatePositionCommand(r.OrgUnitId, r.PositionCode, r.Name, r.Track!.Value,
            r.IsUnitHead!.Value, r.MaxIncumbents, r.LevelId, r.ReportsToPositionId, r.EffectiveFrom, r.Reason, r.DecisionReference),
            Actor(), ct), nameof(GetPosition));

    [HttpPost("positions/{id:guid}/versions")]
    public async Task<IActionResult> AddPositionVersion(Guid id, [FromBody] PositionVersionRequest r, CancellationToken ct) =>
        Mutated(await writes.AddPositionVersionAsync(id, new PositionVersionCommand(r.Name, r.Track!.Value, r.IsUnitHead!.Value, r.MaxIncumbents,
            r.LevelId, r.ReportsToPositionId, r.IsActive!.Value, r.EffectiveFrom, r.Reason, r.DecisionReference), IfMatch(), Actor(), ct));

    // ---- helpers ------------------------------------------------------------------------------------------

    private EmployeeActor Actor()
    {
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        return new EmployeeActor(userId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
    }

    private string? IfMatch()
    {
        var values = Request.Headers.IfMatch;
        if (values.Count == 0) return null;
        if (values.Count > 1 || values[0] is not { } value || value.Contains(',') || value.StartsWith("W/", StringComparison.Ordinal))
            throw new ArgumentException("Supply exactly one strong ETag in If-Match.");
        return value;
    }

    private IActionResult Mutated(OrganizationMutationResult result)
    {
        NoStore();
        Response.Headers.ETag = result.Version;
        return StatusCode(StatusCodes.Status201Created, result);
    }

    private IActionResult Created(OrganizationMutationResult result, string routeName)
    {
        NoStore();
        Response.Headers.ETag = result.Version;
        return CreatedAtRoute(routeName, new { id = result.Id }, result);
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";
}
