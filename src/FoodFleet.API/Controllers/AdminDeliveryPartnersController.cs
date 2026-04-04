using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.DeliveryPartners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin delivery partner management — CRUD and availability toggling.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/delivery-partners")]
[Produces("application/json")]
public class AdminDeliveryPartnersController : ApiControllerBase
{
    /// <summary>List all delivery partners for a branch.</summary>
    /// <param name="branchId"><b>Required.</b> The branch whose partners to list.</param>
    /// <response code="200">Partners returned.</response>
    /// <response code="400"><c>branchId</c> not supplied.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeliveryPartnerDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? branchId, CancellationToken ct)
    {
        if (!branchId.HasValue)
            return BadRequest(new { message = "branchId is required." });
        return ToActionResult(await Mediator.Send(new GetDeliveryPartnersQuery(branchId.Value), ct));
    }

    /// <summary>Register a new delivery partner.</summary>
    /// <response code="201">Partner created.</response>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryPartnerDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryPartnerRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateDeliveryPartnerCommand(request), ct);
        return result.StatusCode == 201
            ? StatusCode(201, result.Data)
            : ToActionResult(result);
    }

    /// <summary>Update a delivery partner's details (name, phone, vehicle).</summary>
    /// <response code="200">Partner updated.</response>
    /// <response code="404">Partner not found.</response>
    [HttpPut("{partnerId:guid}")]
    [ProducesResponseType(typeof(DeliveryPartnerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid partnerId, [FromBody] UpdateDeliveryPartnerRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateDeliveryPartnerCommand(partnerId, request), ct));

    /// <summary>Remove a delivery partner.</summary>
    /// <response code="200">Partner removed.</response>
    /// <response code="404">Partner not found.</response>
    [HttpDelete("{partnerId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid partnerId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new DeleteDeliveryPartnerCommand(partnerId), ct));

    /// <summary>Toggle a delivery partner's availability (available ↔ unavailable).</summary>
    [HttpPatch("{partnerId:guid}/availability")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleAvailability(Guid partnerId, [FromBody] UpdateAvailabilityRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new ToggleDeliveryPartnerAvailabilityCommand(partnerId, request.IsAvailable), ct));
}
