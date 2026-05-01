using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace FoodFleet.Infrastructure.Services;

public class StripePaymentGateway(IConfiguration configuration, ILogger<StripePaymentGateway> logger) : IPaymentGateway
{
    private readonly string _secretKey = configuration["Stripe:SecretKey"]
        ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
    private readonly string _webhookSecret = configuration["Stripe:WebhookSecret"] ?? "";

    public string GatewayName => "Stripe";

    public async Task<PaymentInitResult> InitiateAsync(Order order, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(order.TotalAmount * 100),
            Currency = "inr",
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString(),
                ["customer_id"] = order.CustomerId.ToString()
            },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, cancellationToken: ct);

        logger.LogInformation("Stripe PaymentIntent created: {Id}", intent.Id);

        return new PaymentInitResult(SessionToken: intent.ClientSecret!, RedirectUrl: null, IsImmediate: false);
    }

    public async Task<PaymentConfirmResult> ConfirmAsync(string gatewayRefId, decimal amount, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _secretKey;

        var service = new PaymentIntentService();
        var intent = await service.GetAsync(gatewayRefId, cancellationToken: ct);

        var success = intent.Status == "succeeded";
        logger.LogInformation("Stripe confirm {Id}: {Status}", intent.Id, intent.Status);

        return new PaymentConfirmResult(
            Success: success,
            GatewayRefId: intent.Id,
            ErrorMessage: success ? null : intent.LastPaymentError?.Message);
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return false;
        }
    }
}
