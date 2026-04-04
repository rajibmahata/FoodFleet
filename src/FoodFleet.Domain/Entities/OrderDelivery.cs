using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class OrderDelivery : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid DeliveryPartnerId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

    public Order Order { get; private set; } = default!;
    public DeliveryPartner DeliveryPartner { get; private set; } = default!;

    protected OrderDelivery() { }

    public static OrderDelivery Create(Guid orderId, Guid deliveryPartnerId)
    {
        return new OrderDelivery { OrderId = orderId, DeliveryPartnerId = deliveryPartnerId };
    }
}
