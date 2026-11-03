using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
public class DiscoveryController(IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>
    /// Menyediakan RSA Public Key dalam format standar JWKS (RFC 7517) untuk verifikasi JWT RS256 oleh aplikasi konsumen.
    /// </summary>
    [HttpGet("/.well-known/jwks.json")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JsonWebKeySetResponse), StatusCodes.Status200OK)]
    public ActionResult<JsonWebKeySetResponse> GetJwks()
    {
        var jwks = jwtTokenService.GetJsonWebKeySet();
        return Ok(jwks);
    }
}

