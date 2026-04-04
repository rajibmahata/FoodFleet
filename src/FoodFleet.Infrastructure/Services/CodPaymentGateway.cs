using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Services;

namespace FoodFleet.Infrastructure.Services;

/// <summary>Cash-on-delivery gateway — no external API call required. Order is immediately payable at door.</summary>
public class CodPaymentGateway : IPaymentGateway
{
    public string GatewayName => "COD";

    public Task<PaymentInitResult> InitiateAsync(Order order, CancellationToken ct = default)
    {
        var result = new PaymentInitResult(
            SessionToken: $"COD-{order.Id}",
            RedirectUrl: null,
            IsImmediate: true);
        return Task.FromResult(result);
    }

    public Task<PaymentConfirmResult> ConfirmAsync(string gatewayRefId, decimal amount, CancellationToken ct = default)
    {
        var result = new PaymentConfirmResult(Success: true, GatewayRefId: gatewayRefId, ErrorMessage: null);
        return Task.FromResult(result);
    }

    public bool ValidateWebhookSignature(string payload, string signature) => true;
}
