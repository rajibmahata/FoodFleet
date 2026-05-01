using FoodFleet.Domain.Entities;

namespace FoodFleet.Domain.Interfaces.Services;

public record PaymentInitResult(string SessionToken, string? RedirectUrl, bool IsImmediate);
public record PaymentConfirmResult(bool Success, string? GatewayRefId, string? ErrorMessage);

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentInitResult> InitiateAsync(Order order, CancellationToken ct = default);
    Task<PaymentConfirmResult> ConfirmAsync(string gatewayRefId, decimal amount, CancellationToken ct = default);
    bool ValidateWebhookSignature(string payload, string signature);
}
