using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Enums.Employees;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

public sealed record CreateEmployeeRequest(
    [Required, MaxLength(50)] string EmployeeNumber,
    [Required, MaxLength(200)] string EmployeeFullName,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason);

public sealed record UpdateEmployeeRequest(
    [Required, MaxLength(50)] string EmployeeNumber,
    [Required, MaxLength(200)] string EmployeeFullName,
    [Required, MaxLength(500)] string Reason);

public sealed record ChangeStatusRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] EmployeeStatusCode? StatusCode,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record CreateAssignmentRequest(
    [Required] Guid PositionId,
    [Required] bool? IsPrimary,
    [Required] DateOnly EffectiveFrom,
    DateOnly? EffectiveToExclusive,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

public sealed record TransferRequest(
    [Required] Guid SourceAssignmentId,
    [Required] Guid TargetPositionId,
    [Required] DateOnly EffectiveFrom,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(200)] string? DecisionReference);

/// <summary>Only <c>END</c> is supported in this release; corrections/cancellations are deferred.</summary>
public sealed record AssignmentRevisionRequest(
    [Required, MaxLength(20)] string Action,
    [Required] DateOnly EffectiveToExclusive,
    [Required, MaxLength(500)] string Reason);

public sealed record CreateActingRequest(
    [Required] Guid SourceAssignmentId,
    [Required] Guid TargetPositionId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] [Required] ActingAssignmentType? AssignmentType,
    [Required] DateOnly EffectiveFrom,
    DateOnly? EffectiveToExclusive,
    [Required, MaxLength(200)] string DecisionReference,
    [Required, MaxLength(500)] string Reason);

public sealed record LinkEmployeeRequest(
    [Required] Guid EmployeeId,
    [Required, MaxLength(500)] string Reason);

public sealed record UnlinkEmployeeRequest([Required, MaxLength(500)] string Reason);

/// <summary>
/// Employee profile and placement administration (planning §8.2). Bearer 'auth-admin' only; administrator rights
/// are re-checked against the database inside every service call (D-06: SuperAdmin only in the first release).
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[RequireAdminBearer]
[EnableRateLimiting(RateLimitPolicies.AdminEmployees)]
public sealed class EmployeesAdminController(
    IEmployeeProfileService profiles,
    IEmployeePlacementService placements,
    IEmployeeQueryService queries) : ControllerBase
{
    // ---- reads --------------------------------------------------------------------------------------------

    [HttpGet("employees")]
    [ProducesResponseType(typeof(EmployeePage), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeePage>> List([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        NoStore();
        return Ok(await queries.ListAsync(search, page, pageSize, Actor(), cancellationToken));
    }

    [HttpGet("employees/{id:guid}", Name = nameof(GetEmployee))]
    [ProducesResponseType(typeof(EmployeeDetail), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDetail>> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        NoStore();
        var detail = await queries.GetAsync(id, Actor(), cancellationToken);
        Response.Headers.ETag = detail.Employee.Version;
        return Ok(detail);
    }

    [HttpGet("employees/{id:guid}/history")]
    [ProducesResponseType(typeof(EmployeeHistory), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeHistory>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        NoStore();
        return Ok(await queries.GetHistoryAsync(id, Actor(), cancellationToken));
    }

    // ---- profile ------------------------------------------------------------------------------------------

    [HttpPost("employees")]
    [ProducesResponseType(typeof(EmployeeMutationResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeMutationResult>> Create([FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await profiles.CreateAsync(
            new CreateEmployeeCommand(request.EmployeeNumber, request.EmployeeFullName, request.EffectiveFrom, request.Reason),
            Actor(), cancellationToken);
        return Created(result, CreatedAtRoute(nameof(GetEmployee), new { id = result.Employee.Id }, result));
    }

    [HttpPut("employees/{id:guid}")]
    public async Task<ActionResult<EmployeeMutationResult>> Update(Guid id, [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken) => Mutated(await profiles.UpdateIdentityAsync(id,
        new UpdateEmployeeIdentityCommand(request.EmployeeNumber, request.EmployeeFullName, request.Reason),
        IfMatch(), Actor(), cancellationToken));

    [HttpPost("employees/{id:guid}/status-changes")]
    public async Task<ActionResult<EmployeeMutationResult>> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request,
        CancellationToken cancellationToken) => Mutated(await profiles.ChangeStatusAsync(id,
        new ChangeEmployeeStatusCommand(request.StatusCode!.Value, request.EffectiveFrom, request.Reason, request.DecisionReference),
        IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);

    // ---- placements ---------------------------------------------------------------------------------------

    [HttpPost("employees/{id:guid}/assignments")]
    public async Task<ActionResult<EmployeeMutationResult>> AssignDefinitive(Guid id, [FromBody] CreateAssignmentRequest request,
        CancellationToken cancellationToken) => Mutated(await placements.AssignDefinitiveAsync(id,
        new CreateDefinitiveAssignmentCommand(request.PositionId, request.IsPrimary!.Value, request.EffectiveFrom,
            request.EffectiveToExclusive, request.Reason, request.DecisionReference),
        IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);

    [HttpPost("employees/{id:guid}/transfers")]
    public async Task<ActionResult<EmployeeMutationResult>> Transfer(Guid id, [FromBody] TransferRequest request,
        CancellationToken cancellationToken) => Mutated(await placements.TransferAsync(id,
        new TransferEmployeeCommand(request.SourceAssignmentId, request.TargetPositionId, request.EffectiveFrom,
            request.Reason, request.DecisionReference),
        IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);

    [HttpPost("employees/{id:guid}/assignments/{assignmentId:guid}/revisions")]
    public async Task<ActionResult<EmployeeMutationResult>> ReviseAssignment(Guid id, Guid assignmentId,
        [FromBody] AssignmentRevisionRequest request, CancellationToken cancellationToken)
    {
        RequireEndAction(request.Action);
        return Mutated(await placements.EndDefinitiveAsync(id,
            new EndDefinitiveAssignmentCommand(assignmentId, request.EffectiveToExclusive, request.Reason),
            IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);
    }

    [HttpPost("employees/{id:guid}/acting-assignments")]
    public async Task<ActionResult<EmployeeMutationResult>> AssignActing(Guid id, [FromBody] CreateActingRequest request,
        CancellationToken cancellationToken) => Mutated(await placements.AssignActingAsync(id,
        new CreateActingAssignmentCommand(request.SourceAssignmentId, request.TargetPositionId, request.AssignmentType!.Value,
            request.EffectiveFrom, request.EffectiveToExclusive, request.DecisionReference, request.Reason),
        IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);

    [HttpPost("employees/{id:guid}/acting-assignments/{actingId:guid}/revisions")]
    public async Task<ActionResult<EmployeeMutationResult>> ReviseActing(Guid id, Guid actingId,
        [FromBody] AssignmentRevisionRequest request, CancellationToken cancellationToken)
    {
        RequireEndAction(request.Action);
        return Mutated(await placements.EndActingAsync(id,
            new EndActingAssignmentCommand(actingId, request.EffectiveToExclusive, request.Reason),
            IfMatch(), Actor(), cancellationToken), StatusCodes.Status201Created);
    }

    // ---- account link -------------------------------------------------------------------------------------

    [HttpPut("users/{userId:guid}/employee-link")]
    public async Task<ActionResult<EmployeeMutationResult>> Link(Guid userId, [FromBody] LinkEmployeeRequest request,
        CancellationToken cancellationToken) => Mutated(await profiles.LinkUserAsync(request.EmployeeId, userId,
        request.Reason, IfMatch(), Actor(), cancellationToken));

    [HttpDelete("users/{userId:guid}/employee-link")]
    public async Task<ActionResult<EmployeeMutationResult>> Unlink(Guid userId, [FromBody] UnlinkEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = await queries.FindEmployeeIdByUserAsync(userId, Actor(), cancellationToken)
            ?? throw new KeyNotFoundException("The user is not linked to an employee.");
        return Mutated(await profiles.UnlinkUserAsync(employeeId, userId, request.Reason, IfMatch(), Actor(), cancellationToken));
    }

    // ---- helpers ------------------------------------------------------------------------------------------

    private EmployeeActor Actor()
    {
        // RequireAdminBearer already verified a valid 'sub'; rights are verified from the database by the services.
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        return new EmployeeActor(userId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
    }

    /// <summary>Single strong ETag from If-Match; absence is reported by the service as 428, mismatch as 412.</summary>
    private string? IfMatch()
    {
        var values = Request.Headers.IfMatch;
        if (values.Count == 0) return null;
        if (values.Count > 1 || values[0] is not { } value || value.Contains(',') || value.StartsWith("W/", StringComparison.Ordinal))
            throw new ArgumentException("Supply exactly one strong ETag in If-Match.");
        return value;
    }

    private ActionResult<EmployeeMutationResult> Mutated(EmployeeMutationResult result, int status = StatusCodes.Status200OK)
    {
        NoStore();
        Response.Headers.ETag = result.Employee.Version;
        return StatusCode(status, result);
    }

    private ActionResult<EmployeeMutationResult> Created(EmployeeMutationResult result, CreatedAtRouteResult created)
    {
        NoStore();
        Response.Headers.ETag = result.Employee.Version;
        return created;
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";

    private static void RequireEndAction(string action)
    {
        if (!string.Equals(action, "END", StringComparison.Ordinal))
            throw new ArgumentException("Only the END action is supported in this release.");
    }
}
