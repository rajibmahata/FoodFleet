using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Orders;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Enums;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IMenuItemRepository> _menuItemRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IGeoService> _geoService = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid MenuItemId = Guid.NewGuid();

    public PlaceOrderCommandHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.MenuItems).Returns(_menuItemRepo.Object);
        _uow.Setup(u => u.Orders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.Payments).Returns(_paymentRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationService.Setup(n => n.SendOrderConfirmedAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_uow.Object, _geoService.Object, _notificationService.Object);

    private Branch MakeBranch(double radiusKm = 10)
    {
        return Branch.Create(Guid.NewGuid(), "Test Branch", 12.9, 77.6, "1 Main Rd", radiusKm);
    }

    private MenuItem MakeMenuItem(bool isAvailable = true, int? stock = null)
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Burger", 10.00m, stockCount: stock);
        if (!isAvailable) item.SetAvailability(false);
        return item;
    }

    private PlaceOrderCommand MakeCommand(double deliveryLat = 12.9, double deliveryLng = 77.6) =>
        new(CustomerId, new PlaceOrderRequest(
            BranchId,
            "COD",
            "123 Test St",
            deliveryLat,
            deliveryLng,
            [new OrderItemRequest(MenuItemId, null, 1)]));

    // ── Happy Path ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCODOrder_Returns201Order()
    {
        var branch = MakeBranch(radiusKm: 20);
        var menuItem = MakeMenuItem();

        _branchRepo.Setup(r => r.GetByIdAsync(BranchId, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>())).Returns(5.0); // within radius
        _menuItemRepo.Setup(r => r.GetWithVariantsAsync(MenuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menuItem);
        _menuItemRepo.Setup(r => r.GetByIdAsync(MenuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menuItem);
        _menuItemRepo.Setup(r => r.Update(It.IsAny<MenuItem>()));
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
    }

    // ── Geo Validation ────────────────────────────────────────

    [Fact]
    public async Task Handle_DeliveryOutsideRadius_Returns422()
    {
        var branch = MakeBranch(radiusKm: 5);
        _branchRepo.Setup(r => r.GetByIdAsync(BranchId, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>())).Returns(10.0); // outside 5km radius

        var result = await CreateHandler().Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Error.Should().Contain("outside the branch delivery radius");
    }

    // ── Branch Not Found ──────────────────────────────────────

    [Fact]
    public async Task Handle_BranchNotFound_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(BranchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await CreateHandler().Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ── Unavailable Item ──────────────────────────────────────

    [Fact]
    public async Task Handle_UnavailableMenuItem_Returns400()
    {
        var branch = MakeBranch(radiusKm: 20);
        var unavailableItem = MakeMenuItem(isAvailable: false);

        _branchRepo.Setup(r => r.GetByIdAsync(BranchId, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>())).Returns(1.0);
        _menuItemRepo.Setup(r => r.GetWithVariantsAsync(MenuItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unavailableItem);

        var result = await CreateHandler().Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not available");
    }

    // ── Invalid Payment Method ─────────────────────────────────

    [Fact]
    public async Task Handle_InvalidPaymentMethod_Returns400()
    {
        var branch = MakeBranch(radiusKm: 20);
        _branchRepo.Setup(r => r.GetByIdAsync(BranchId, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>())).Returns(1.0);

        var command = new PlaceOrderCommand(CustomerId, new PlaceOrderRequest(
            BranchId,
            "BITCOIN",  // invalid
            "123 Test St", 12.9, 77.6,
            [new OrderItemRequest(MenuItemId, null, 1)]));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid payment method");
    }
}

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<INotificationService> _notificationService = new();

    public CancelOrderCommandHandlerTests()
    {
        _uow.Setup(u => u.Orders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _orderRepo.Setup(r => r.Update(It.IsAny<Order>()));
        _notificationService.Setup(n => n.SendOrderCancelledAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CancelOrderCommandHandler CreateHandler() => new(_uow.Object, _notificationService.Object);

    private static Order MakeConfirmedOrder(Guid customerId)
    {
        var items = new List<OrderItem> { OrderItem.Create(Guid.NewGuid(), null, 1, 10m) };
        return Order.Create(customerId, Guid.NewGuid(), PaymentMethod.COD, "Addr", 0, 0, items);
    }

    [Fact]
    public async Task Handle_CustomerCancelsOwnOrder_Succeeds()
    {
        var customerId = Guid.NewGuid();
        var order = MakeConfirmedOrder(customerId);
        _orderRepo.Setup(r => r.GetWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(
            new CancelOrderCommand(order.Id, customerId, "Changed mind"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CustomerCancelsOtherCustomerOrder_Returns403()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var order = MakeConfirmedOrder(ownerId);
        _orderRepo.Setup(r => r.GetWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(
            new CancelOrderCommand(order.Id, attackerId, "Trying to cancel other's order"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Returns404()
    {
        _orderRepo.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(
            new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid(), "reason"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
