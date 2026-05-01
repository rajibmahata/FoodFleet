using FoodFleet.Domain.Entities;
using FluentAssertions;

namespace FoodFleet.Tests.Unit.Domain;

public class MenuItemTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsMenuItem()
    {
        var item = MenuItem.Create(CategoryId, "Burger", 9.99m);

        item.Name.Should().Be("Burger");
        item.Price.Should().Be(9.99m);
        item.CategoryId.Should().Be(CategoryId);
        item.IsAvailable.Should().BeTrue();
        item.StockCount.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => MenuItem.Create(CategoryId, "   ", 10m);
        act.Should().Throw<ArgumentException>().WithMessage("*name is required*");
    }

    [Fact]
    public void Create_NegativePrice_Throws()
    {
        var act = () => MenuItem.Create(CategoryId, "Soup", -1m);
        act.Should().Throw<ArgumentException>().WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void Create_ZeroPrice_IsAllowed()
    {
        var item = MenuItem.Create(CategoryId, "Free sample", 0m);
        item.Price.Should().Be(0m);
    }

    // ── SetAvailability ───────────────────────────────────────

    [Fact]
    public void SetAvailability_False_MarksUnavailable()
    {
        var item = MenuItem.Create(CategoryId, "Pizza", 15m);
        item.SetAvailability(false);
        item.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void SetAvailability_True_MarksAvailable()
    {
        var item = MenuItem.Create(CategoryId, "Pizza", 15m);
        item.SetAvailability(false);
        item.SetAvailability(true);
        item.IsAvailable.Should().BeTrue();
    }

    // ── UpdateStock ───────────────────────────────────────────

    [Fact]
    public void UpdateStock_PositiveCount_SetsCount()
    {
        var item = MenuItem.Create(CategoryId, "Nachos", 8m);
        item.UpdateStock(50);
        item.StockCount.Should().Be(50);
        item.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void UpdateStock_Zero_MarksUnavailable()
    {
        var item = MenuItem.Create(CategoryId, "Limited", 12m);
        item.UpdateStock(0);
        item.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void UpdateStock_Null_SetsUnlimitedStock()
    {
        var item = MenuItem.Create(CategoryId, "Drink", 3m, stockCount: 5);
        item.UpdateStock(null);
        item.StockCount.Should().BeNull();
    }

    // ── DecrementStock ────────────────────────────────────────

    [Fact]
    public void DecrementStock_WithStock_DecrementsCount()
    {
        var item = MenuItem.Create(CategoryId, "Cake", 20m, stockCount: 5);
        item.DecrementStock();
        item.StockCount.Should().Be(4);
        item.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void DecrementStock_LastOne_MarksUnavailable()
    {
        var item = MenuItem.Create(CategoryId, "LastSlice", 20m, stockCount: 1);
        item.DecrementStock();
        item.StockCount.Should().Be(0);
        item.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void DecrementStock_UnlimitedStock_NoChange()
    {
        var item = MenuItem.Create(CategoryId, "Unlimited", 5m); // null stock
        item.DecrementStock();
        item.StockCount.Should().BeNull(); // unchanged
        item.IsAvailable.Should().BeTrue();
    }
}
