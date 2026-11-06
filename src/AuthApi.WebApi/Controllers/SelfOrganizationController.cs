using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using AuthApi.Application.Features.Employees;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

/// <summary>
/// Organization profile of the signed-in account (planning §8.1). The user comes from the token subject and the application
/// from its audience; there is no userId/asOf parameter, no listing of other people, and no permission data in the response.
/// Account, application, role and employee status are re-read from the database on every request.
/// </summary>
[ApiController]
[Route("api/v1/auth/me/organization")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.SelfProfile)]
public sealed class SelfOrganizationController(ISelfOrganizationProfileService profiles) : ControllerBase
{
    /// <summary>Own organization profile effective today (Asia/Jakarta). Supports conditional polling via If-None-Match.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SelfOrganizationProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<SelfOrganizationProfileDto>> Get(CancellationToken cancellationToken)
    {
        var (userId, clientId) = Caller();
        var profile = await profiles.GetAsync(userId, clientId, cancellationToken);
        var etag = '"' + profile.ProfileVersion + '"';
        Response.Headers.CacheControl = "no-store";
        Response.Headers.ETag = etag;
        if (Request.Headers.IfNoneMatch.Any(x => x is not null && x.Split(',').Select(t => t.Trim()).Any(t => t == "*" || (t.StartsWith("W/", StringComparison.Ordinal) ? t[2..] : t) == etag)))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(profile);
    }

    /// <summary>Formal reporting line (positions and vacancy, never people) of one of the caller's own assignments.</summary>
    [HttpGet("reporting-line")]
    [ProducesResponseType(typeof(SelfReportingLineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SelfReportingLineDto>> ReportingLine([FromQuery, Required] Guid? assignmentId,
        CancellationToken cancellationToken)
    {
        var (userId, clientId) = Caller();
        Response.Headers.CacheControl = "no-store";
        return Ok(await profiles.GetReportingLineAsync(userId, clientId, assignmentId!.Value, cancellationToken));
    }

    private (Guid UserId, string ClientId) Caller()
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            throw new UnauthorizedAccessException("Token tidak memuat subject pengguna yang valid.");
        var clientId = User.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
        if (string.IsNullOrWhiteSpace(clientId))
            throw new UnauthorizedAccessException("Klaim audience (clientId) tidak ditemukan pada token.");
        return (userId, clientId);
    }
}
