using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Branches;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Public branch endpoints — no authentication required.</summary>
[Produces("application/json")]
public class BranchesController : ApiControllerBase
{
    /// <summary>List all active branches.</summary>
    /// <response code="200">Branches returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BranchDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetAllBranchesQuery(), ct));

    /// <summary>Find the nearest branch to the given GPS coordinates.</summary>
    /// <param name="lat">Latitude of the customer's location.</param>
    /// <param name="lng">Longitude of the customer's location.</param>
    /// <response code="200">Nearest branch found.</response>
    /// <response code="404">No active branch available within range.</response>
    [HttpGet("nearest")]
    [ProducesResponseType(typeof(BranchDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetNearest([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetNearestBranchQuery(lat, lng), ct));

    /// <summary>Get branch details including its delivery radius.</summary>
    /// <response code="200">Branch returned.</response>
    /// <response code="404">Branch not found.</response>
    [HttpGet("{branchId:guid}")]
    [ProducesResponseType(typeof(BranchDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetBranchByIdQuery(branchId), ct));

    /// <summary>Get the full public menu for a branch (available items only).</summary>
    /// <response code="200">Menu returned.</response>
    /// <response code="404">Branch not found.</response>
    [HttpGet("{branchId:guid}/menu")]
    [ProducesResponseType(typeof(BranchMenuResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMenu(Guid branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetBranchMenuQuery(branchId), ct));
}
