using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Orders;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Customer order operations — place, track and cancel orders. Customer JWT required.</summary>
[Authorize]
[Produces("application/json")]
public class OrdersController(ICurrentUserService currentUser) : ApiControllerBase
{
    /// <summary>Place a new order.</summary>
    /// <remarks>
    /// The delivery address must fall within the selected branch's configured radius.  
    /// Use <c>POST /api/geo/validate-delivery</c> to pre-validate before calling this.
    /// </remarks>
    /// <response code="201">Order placed and payment pending.</response>
    /// <response code="422">Delivery address is outside service radius.</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), 201)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new PlaceOrderCommand(currentUser.CustomerId!.Value, request), ct);
        return result.StatusCode == 201
            ? StatusCode(201, result.Data)
            : ToActionResult(result);
    }

    /// <summary>Get order details by ID.</summary>
    /// <response code="200">Order returned.</response>
    /// <response code="403">Order belongs to a different customer.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetOrderByIdQuery(orderId, currentUser.CustomerId), ct));

    /// <summary>Get the authenticated customer's full order history.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetOrderHistoryQuery(currentUser.CustomerId!.Value), ct));

    /// <summary>Cancel an order (only permitted while the order is still <c>Pending</c>).</summary>
    /// <response code="200">Order cancelled.</response>
    /// <response code="422">Order cannot be cancelled in its current state.</response>
    [HttpPost("{orderId:guid}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Cancel(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new CancelOrderCommand(orderId, currentUser.CustomerId!.Value, request.Reason), ct));
}
