using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Enums;
using FluentAssertions;

namespace FoodFleet.Tests.Unit.Domain;

public class OrderTests
{
    private static List<OrderItem> OneItem() =>
        [OrderItem.Create(Guid.NewGuid(), null, 1, 10.00m)];

    // ── Creation ──────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsOrder()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "123 Main St", 12.9, 77.6, OneItem());

        order.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Confirmed); // COD starts Confirmed
        order.TotalAmount.Should().Be(10.00m);
    }

    [Fact]
    public void Create_COD_StartsAsConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, OneItem());

        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Create_UPI_StartsAsPendingPayment()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.UPI,
            "Addr", 0, 0, OneItem());

        order.Status.Should().Be(OrderStatus.PendingPayment);
    }

    [Fact]
    public void Create_EmptyItems_Throws()
    {
        var act = () => Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, []);

        act.Should().Throw<ArgumentException>().WithMessage("*at least one item*");
    }

    // ── Status Transitions ────────────────────────────────────

    [Fact]
    public void UpdateStatus_ValidTransition_UpdatesStatus()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, OneItem());

        order.UpdateStatus(OrderStatus.Preparing);

        order.Status.Should().Be(OrderStatus.Preparing);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Preparing, OrderStatus.OutForDelivery)]
    [InlineData(OrderStatus.OutForDelivery, OrderStatus.Delivered)]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Confirmed)]
    public void UpdateStatus_AllValidHappyPathTransitions_Succeed(OrderStatus from, OrderStatus to)
    {
        // Build order in the right starting state
        var order = BuildOrderInStatus(from);
        order.UpdateStatus(to);
        order.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Preparing)]
    [InlineData(OrderStatus.OutForDelivery, OrderStatus.Confirmed)]
    public void UpdateStatus_InvalidTransition_Throws(OrderStatus from, OrderStatus to)
    {
        var order = BuildOrderInStatus(from);
        var act = () => order.UpdateStatus(to);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot transition*");
    }

    // ── Cancel ────────────────────────────────────────────────

    [Fact]
    public void Cancel_ConfirmedOrder_Cancels()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, OneItem());

        order.Cancel("Changed my mind");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Changed my mind");
    }

    [Fact]
    public void Cancel_DeliveredOrder_Throws()
    {
        var order = BuildOrderInStatus(OrderStatus.Delivered);
        var act = () => order.Cancel("Too late");
        act.Should().Throw<InvalidOperationException>().WithMessage("*delivered*");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, OneItem());
        order.Cancel("First cancel");

        var act = () => order.Cancel("Second cancel");
        act.Should().Throw<InvalidOperationException>().WithMessage("*already cancelled*");
    }

    // ── TotalAmount ───────────────────────────────────────────

    [Fact]
    public void TotalAmount_IsSum_OfAllItems()
    {
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), null, 2, 50.00m),  // 100
            OrderItem.Create(Guid.NewGuid(), null, 1, 30.00m),  // 30
        };
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, items);

        order.TotalAmount.Should().Be(130.00m);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static Order BuildOrderInStatus(OrderStatus target)
    {
        if (target == OrderStatus.PendingPayment)
        {
            // Use UPI to start in PendingPayment state
            return Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.UPI,
                "Addr", 0, 0, OneItem());
        }

        // Start from Confirmed (COD) and advance
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.COD,
            "Addr", 0, 0, OneItem());
        // order starts as Confirmed

        if (target == OrderStatus.Confirmed) return order;

        if (target == OrderStatus.Cancelled)
        {
            order.Cancel("test");
            return order;
        }

        var sequence = new[] { OrderStatus.Preparing, OrderStatus.OutForDelivery, OrderStatus.Delivered };
        foreach (var s in sequence)
        {
            order.UpdateStatus(s);
            if (order.Status == target) break;
        }

        return order;
    }
}
