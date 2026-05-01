using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Settings;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Public restaurant information — no authentication required.</summary>
[Route("api/restaurant")]
[Produces("application/json")]
public class RestaurantController : ApiControllerBase
{
    /// <summary>Returns the restaurant's display name, logo URL and contact email.</summary>
    /// <response code="200">Restaurant info returned.</response>
    /// <response code="404">No active restaurant configured.</response>
    [HttpGet("info")]
    [ProducesResponseType(typeof(RestaurantInfoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInfo(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetRestaurantInfoQuery(), ct));
}
