using FoodFleet.Domain.Common;
using FoodFleet.Domain.Enums;

namespace FoodFleet.Domain.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid BranchId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string DeliveryAddress { get; private set; } = default!;
    public double DeliveryLat { get; private set; }
    public double DeliveryLng { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime PlacedAt { get; private set; } = DateTime.UtcNow;

    public Customer Customer { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Payment? Payment { get; private set; }
    public OrderDelivery? Delivery { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    protected Order() { }

    public static Order Create(Guid customerId, Guid branchId, PaymentMethod paymentMethod,
        string deliveryAddress, double deliveryLat, double deliveryLng, List<OrderItem> items)
    {
        if (!items.Any()) throw new ArgumentException("Order must have at least one item.", nameof(items));

        var order = new Order
        {
            CustomerId = customerId,
            BranchId = branchId,
            PaymentMethod = paymentMethod,
            DeliveryAddress = deliveryAddress,
            DeliveryLat = deliveryLat,
            DeliveryLng = deliveryLng,
            Status = paymentMethod == PaymentMethod.COD ? OrderStatus.Confirmed : OrderStatus.PendingPayment
        };

        order._items.AddRange(items);
        order.TotalAmount = items.Sum(i => i.TotalPrice);
        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        ValidateStatusTransition(Status, newStatus);
        Status = newStatus;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered order.");
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        SetUpdated();
    }

    private static void ValidateStatusTransition(OrderStatus current, OrderStatus next)
    {
        var validTransitions = new Dictionary<OrderStatus, HashSet<OrderStatus>>
        {
            [OrderStatus.PendingPayment] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed]      = new() { OrderStatus.Preparing, OrderStatus.Cancelled },
            [OrderStatus.Preparing]      = new() { OrderStatus.OutForDelivery, OrderStatus.Cancelled },
            [OrderStatus.OutForDelivery] = new() { OrderStatus.Delivered },
            [OrderStatus.Delivered]      = new(),
            [OrderStatus.Cancelled]      = new()
        };

        if (!validTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidOperationException($"Cannot transition order from {current} to {next}.");
    }
}
