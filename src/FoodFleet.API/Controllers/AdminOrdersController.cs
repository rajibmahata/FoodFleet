using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin order management — view, filter, update status, assign delivery partners.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/orders")]
[Produces("application/json")]
public class AdminOrdersController : ApiControllerBase
{
    /// <summary>List orders with optional filters.</summary>
    /// <param name="branchId">Filter by branch.</param>
    /// <param name="status">Filter by order status (e.g. <c>Pending</c>, <c>Preparing</c>, <c>Delivered</c>).</param>
    /// <param name="from">Start of date range (UTC).</param>
    /// <param name="to">End of date range (UTC).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? branchId, [FromQuery] string? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
        => ToActionResult(await Mediator.Send(new GetAdminOrdersQuery(branchId, status, from, to), ct));

    /// <summary>Update the status of an order (e.g. Pending → Preparing → OutForDelivery → Delivered).</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="404">Order not found.</response>
    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateOrderStatusCommand(orderId, request.Status), ct));

    /// <summary>Assign a delivery partner to a confirmed order.</summary>
    /// <response code="200">Partner assigned.</response>
    /// <response code="404">Order or partner not found.</response>
    [HttpPatch("{orderId:guid}/assign-partner")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignPartner(Guid orderId, [FromBody] AssignDeliveryPartnerRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new AssignDeliveryPartnerCommand(orderId, request.DeliveryPartnerId), ct));
}
