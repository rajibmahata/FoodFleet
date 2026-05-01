using FoodFleet.Domain.Entities;
using FluentAssertions;

namespace FoodFleet.Tests.Unit.Domain;

public class BranchTests
{
    private static readonly Guid RestaurantId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsBranch()
    {
        var branch = Branch.Create(RestaurantId, "Downtown", 12.9, 77.6, "1 Main St", 5.0);

        branch.Name.Should().Be("Downtown");
        branch.Lat.Should().Be(12.9);
        branch.Lng.Should().Be(77.6);
        branch.Address.Should().Be("1 Main St");
        branch.DeliveryRadiusKm.Should().Be(5.0);
        branch.IsActive.Should().BeTrue();
        branch.RestaurantId.Should().Be(RestaurantId);
    }

    [Fact]
    public void Create_DefaultRadius_IsThreeKm()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr");
        branch.DeliveryRadiusKm.Should().Be(3.0);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => Branch.Create(RestaurantId, "", 0, 0, "Addr");
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_EmptyAddress_Throws()
    {
        var act = () => Branch.Create(RestaurantId, "Name", 0, 0, "");
        act.Should().Throw<ArgumentException>().WithMessage("*address*");
    }

    // ── Update ────────────────────────────────────────────────

    [Fact]
    public void Update_ChangesAllFields()
    {
        var branch = Branch.Create(RestaurantId, "Old", 1.0, 2.0, "Old Addr", 3.0);
        branch.Update("New", 5.0, 6.0, "New Addr", 7.0);

        branch.Name.Should().Be("New");
        branch.Lat.Should().Be(5.0);
        branch.Lng.Should().Be(6.0);
        branch.Address.Should().Be("New Addr");
        branch.DeliveryRadiusKm.Should().Be(7.0);
    }

    // ── UpdateDeliveryRadius ──────────────────────────────────

    [Fact]
    public void UpdateDeliveryRadius_SetsNewRadius()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr", 5.0);
        branch.UpdateDeliveryRadius(12.5);
        branch.DeliveryRadiusKm.Should().Be(12.5);
    }

    [Fact]
    public void UpdateDeliveryRadius_Zero_Throws()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr", 5.0);
        var act = () => branch.UpdateDeliveryRadius(0);
        act.Should().Throw<ArgumentException>().WithMessage("*radius*");
    }

    [Fact]
    public void UpdateDeliveryRadius_Negative_Throws()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr", 5.0);
        var act = () => branch.UpdateDeliveryRadius(-1.0);
        act.Should().Throw<ArgumentException>().WithMessage("*radius*");
    }

    // ── Deactivate / Activate ─────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr");
        branch.Deactivate();
        branch.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var branch = Branch.Create(RestaurantId, "Branch", 0, 0, "Addr");
        branch.Deactivate();
        branch.Activate();
        branch.IsActive.Should().BeTrue();
    }
}
