using FoodFleet.Domain.Enums;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodFleet.Infrastructure.Services;

/// <summary>Resolves the correct IPaymentGateway implementation for a given PaymentMethod.</summary>
public class PaymentGatewayFactory(IServiceProvider serviceProvider)
{
    public IPaymentGateway Resolve(PaymentMethod method) => method switch
    {
        PaymentMethod.COD => serviceProvider.GetRequiredService<CodPaymentGateway>(),
        PaymentMethod.Stripe => serviceProvider.GetRequiredService<StripePaymentGateway>(),
        PaymentMethod.Razorpay => serviceProvider.GetRequiredService<RazorpayPaymentGateway>(),
        PaymentMethod.UPI => serviceProvider.GetRequiredService<RazorpayPaymentGateway>(),
        _ => throw new NotSupportedException($"Payment method {method} is not supported.")
    };
}
