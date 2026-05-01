using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Geo;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Geo-validation — check whether a delivery address is within a branch's service radius.</summary>
[Produces("application/json")]
public class GeoController : ApiControllerBase
{
    /// <summary>Validate a delivery address against the branch's configured delivery radius.</summary>
    /// <remarks>
    /// Uses the **Haversine formula** to compute great-circle distance.  
    /// Returns <c>isDeliverable: false</c> (HTTP 200) when outside range — the order controller 
    /// will additionally return HTTP 422 if placement is attempted from outside the radius.
    /// </remarks>
    /// <response code="200">Validation result returned.</response>
    /// <response code="404">Branch not found.</response>
    [HttpPost("validate-delivery")]
    [ProducesResponseType(typeof(GeoValidationResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ValidateDelivery([FromBody] ValidateDeliveryRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new ValidateDeliveryQuery(request.BranchId, request.DeliveryLat, request.DeliveryLng), ct));
}
