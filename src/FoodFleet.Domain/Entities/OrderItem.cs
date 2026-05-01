using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public Guid? VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public Order Order { get; private set; } = default!;
    public MenuItem MenuItem { get; private set; } = default!;
    public MenuItemVariant? Variant { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    protected OrderItem() { }

    public static OrderItem Create(Guid menuItemId, Guid? variantId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new OrderItem
        {
            MenuItemId = menuItemId,
            VariantId = variantId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal void SetOrderId(Guid orderId) => OrderId = orderId;
}
