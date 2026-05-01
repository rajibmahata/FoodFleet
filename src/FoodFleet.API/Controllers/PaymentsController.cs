using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Payments;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Customer payment operations — initiate, confirm, and handle gateway webhooks.</summary>
[Authorize]
[Produces("application/json")]
public class PaymentsController(ICurrentUserService currentUser) : ApiControllerBase
{
    /// <summary>Initiate payment for an order. Returns a gateway-specific payment URL or token.</summary>
    /// <remarks>Supported gateways: <c>razorpay</c>, <c>stripe</c>, <c>upi</c>, <c>cod</c>.</remarks>
    /// <response code="200">Payment session created — redirect URL or token returned.</response>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PaymentInitResponse), 200)]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new InitiatePaymentCommand(currentUser.CustomerId!.Value, request), ct));

    /// <summary>Confirm payment after successful callback from the payment gateway.</summary>
    /// <response code="200">Payment confirmed and order status updated.</response>
    /// <response code="400">Invalid payment or already confirmed.</response>
    [HttpPost("confirm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new ConfirmPaymentCommand(currentUser.CustomerId!.Value, request), ct));

    /// <summary>Payment gateway webhook receiver. <b>No JWT required</b> — verified by gateway signature header.</summary>
    /// <param name="gateway">Gateway name: <c>razorpay</c> or <c>stripe</c>.</param>
    /// <response code="200">Webhook processed.</response>
    /// <response code="400">Signature verification failed.</response>
    [AllowAnonymous]
    [HttpPost("webhook/{gateway}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Webhook(string gateway, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? "";

        return ToActionResult(await Mediator.Send(new HandleWebhookCommand(gateway, payload, signature), ct));
    }
}
