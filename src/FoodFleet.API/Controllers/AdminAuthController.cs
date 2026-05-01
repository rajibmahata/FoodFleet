using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodFleet.API.Controllers;

/// <summary>Admin authentication — issues admin-scoped JWTs with Admin/SuperAdmin roles.</summary>
[EnableRateLimiting("auth")]
[Route("api/auth/admin")]
[Produces("application/json")]
public class AdminAuthController : ApiControllerBase
{
    /// <summary>Admin login. Returns a short-lived access token and a refresh token.</summary>
    /// <remarks>Use the returned <c>accessToken</c> as a Bearer token on all <b>Admin</b> endpoints.</remarks>
    /// <response code="200">Login successful — token pair returned.</response>
    /// <response code="401">Invalid username or password.</response>
    /// <response code="429">Too many requests — rate limit exceeded.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AdminAuthResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new AdminLoginCommand(request), ct));
}
