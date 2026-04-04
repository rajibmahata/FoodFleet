using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoodFleet.Infrastructure.Services;

public class RazorpayPaymentGateway(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RazorpayPaymentGateway> logger) : IPaymentGateway
{
    private readonly string _keyId = configuration["Razorpay:KeyId"]
        ?? throw new InvalidOperationException("Razorpay:KeyId is not configured.");
    private readonly string _keySecret = configuration["Razorpay:KeySecret"]
        ?? throw new InvalidOperationException("Razorpay:KeySecret is not configured.");
    private readonly string _webhookSecret = configuration["Razorpay:WebhookSecret"] ?? "";

    public string GatewayName => "Razorpay";

    public async Task<PaymentInitResult> InitiateAsync(Order order, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("razorpay");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var body = new
        {
            amount = (long)(order.TotalAmount * 100),
            currency = "INR",
            receipt = order.Id.ToString(),
            notes = new { order_id = order.Id.ToString(), customer_id = order.CustomerId.ToString() }
        };

        var response = await client.PostAsJsonAsync("https://api.razorpay.com/v1/orders", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var razorOrderId = doc.RootElement.GetProperty("id").GetString()!;

        logger.LogInformation("Razorpay order created: {Id}", razorOrderId);

        return new PaymentInitResult(SessionToken: razorOrderId, RedirectUrl: null, IsImmediate: false);
    }

    public async Task<PaymentConfirmResult> ConfirmAsync(string gatewayRefId, decimal amount, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("razorpay");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var response = await client.GetAsync($"https://api.razorpay.com/v1/payments/{gatewayRefId}", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString();

        logger.LogInformation("Razorpay payment {Id}: {Status}", gatewayRefId, status);

        return new PaymentConfirmResult(
            Success: status == "captured",
            GatewayRefId: gatewayRefId,
            ErrorMessage: status == "captured" ? null : $"Payment status: {status}");
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_webhookSecret)) return false;

        var keyBytes = Encoding.UTF8.GetBytes(_webhookSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var expectedHex = Convert.ToHexString(HMACSHA256.HashData(keyBytes, payloadBytes)).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHex),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
