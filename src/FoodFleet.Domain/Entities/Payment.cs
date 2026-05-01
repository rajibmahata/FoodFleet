using FoodFleet.Domain.Common;
using FoodFleet.Domain.Enums;

namespace FoodFleet.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Gateway { get; private set; } = default!;
    public string? GatewayRefId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public Order Order { get; private set; } = default!;

    protected Payment() { }

    public static Payment Create(Guid orderId, string gateway, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(gateway)) throw new ArgumentException("Gateway is required.", nameof(gateway));
        if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));

        return new Payment { OrderId = orderId, Gateway = gateway, Amount = amount };
    }

    public void MarkCompleted(string gatewayRefId)
    {
        GatewayRefId = gatewayRefId;
        Status = PaymentStatus.Completed;
        PaidAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void MarkFailed() { Status = PaymentStatus.Failed; SetUpdated(); }

    public void MarkRefunded() { Status = PaymentStatus.Refunded; SetUpdated(); }

    public void SetGatewayRef(string refId) { GatewayRefId = refId; SetUpdated(); }
}
