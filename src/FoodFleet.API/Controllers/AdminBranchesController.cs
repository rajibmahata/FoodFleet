using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin branch management — CRUD and delivery radius configuration.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/branches")]
[Produces("application/json")]
public class AdminBranchesController : ApiControllerBase
{
    /// <summary>List all branches (active and inactive).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BranchDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetAllBranchesQuery(), ct));

    /// <summary>Create a new branch.</summary>
    /// <response code="201">Branch created.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BranchDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateBranchCommand(request), ct);
        return result.StatusCode == 201
            ? StatusCode(201, result.Data)
            : ToActionResult(result);
    }

    /// <summary>Update a branch's name, coordinates and address.</summary>
    /// <response code="200">Branch updated.</response>
    /// <response code="404">Branch not found.</response>
    [HttpPut("{branchId:guid}")]
    [ProducesResponseType(typeof(BranchDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid branchId, [FromBody] UpdateBranchRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateBranchCommand(branchId, request), ct));

    /// <summary>Deactivate a branch (soft delete).</summary>
    /// <response code="200">Branch deactivated.</response>
    /// <response code="404">Branch not found.</response>
    [HttpDelete("{branchId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new DeleteBranchCommand(branchId), ct));

    /// <summary>Update the delivery radius for a branch (in kilometres).</summary>
    /// <response code="200">Radius updated.</response>
    /// <response code="404">Branch not found.</response>
    [HttpPut("{branchId:guid}/delivery-radius")]
    [ProducesResponseType(typeof(BranchDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDeliveryRadius(Guid branchId, [FromBody] UpdateDeliveryRadiusRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateDeliveryRadiusCommand(branchId, request.RadiusKm), ct));
}
