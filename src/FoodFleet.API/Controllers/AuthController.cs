using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodFleet.API.Controllers;

/// <summary>Customer authentication — register, login, and token refresh.</summary>
[EnableRateLimiting("auth")]
[Produces("application/json")]
public class AuthController : ApiControllerBase
{
    /// <summary>Register a new customer account.</summary>
    /// <response code="201">Account created — token pair returned.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RegisterCommand(request), ct);
        return result.StatusCode == 201
            ? StatusCode(201, result.Data)
            : ToActionResult(result);
    }

    /// <summary>Customer login. Returns access + refresh tokens.</summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new LoginCommand(request), ct));

    /// <summary>Rotate access token using a valid refresh token.</summary>
    /// <response code="200">New token pair issued.</response>
    /// <response code="401">Refresh token invalid or expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new RefreshTokenCommand(request), ct));
}
