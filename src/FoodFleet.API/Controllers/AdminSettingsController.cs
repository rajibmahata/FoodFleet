using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin settings — key/value configuration for the restaurant (name, logo, tax rate, etc.).</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/settings")]
[Produces("application/json")]
public class AdminSettingsController : ApiControllerBase
{
    /// <summary>Get all settings for the active restaurant.</summary>
    /// <response code="200">Settings dictionary returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SettingDto>), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetActiveRestaurantSettingsQuery(), ct));

    /// <summary>Bulk upsert settings for the active restaurant.</summary>
    /// <remarks>Creates or updates each key in the supplied dictionary.</remarks>
    /// <response code="200">Settings saved.</response>
    [HttpPut]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Upsert([FromBody] BulkUpsertSettingsRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpsertActiveRestaurantSettingsCommand(request.Settings), ct));

    /// <summary>Get settings for a specific restaurant by ID.</summary>
    /// <response code="200">Settings returned.</response>
    /// <response code="404">Restaurant not found.</response>
    [HttpGet("{restaurantId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SettingDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid restaurantId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetSettingsQuery(restaurantId), ct));

    /// <summary>Bulk upsert settings for a specific restaurant by ID.</summary>
    /// <response code="200">Settings saved.</response>
    [HttpPut("{restaurantId:guid}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpsertById(Guid restaurantId, [FromBody] BulkUpsertSettingsRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpsertSettingsCommand(restaurantId, request.Settings), ct));

    /// <summary>Get the restaurant's public info (name, logo, contact) from settings.</summary>
    /// <response code="200">Restaurant info returned.</response>
    [HttpGet("restaurant-info")]
    [ProducesResponseType(typeof(RestaurantInfoDto), 200)]
    public async Task<IActionResult> GetRestaurantInfo(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetRestaurantInfoQuery(), ct));
}
