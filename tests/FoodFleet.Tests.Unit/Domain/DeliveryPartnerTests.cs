using FoodFleet.Domain.Entities;
using FluentAssertions;

namespace FoodFleet.Tests.Unit.Domain;

public class DeliveryPartnerTests
{
    private static readonly Guid BranchId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ReturnsPartner()
    {
        var partner = DeliveryPartner.Create(BranchId, "Rahul", "9876543210");

        partner.Name.Should().Be("Rahul");
        partner.Phone.Should().Be("9876543210");
        partner.BranchId.Should().Be(BranchId);
        partner.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => DeliveryPartner.Create(BranchId, "", "1234567890");
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_EmptyPhone_Throws()
    {
        var act = () => DeliveryPartner.Create(BranchId, "Rahul", "");
        act.Should().Throw<ArgumentException>().WithMessage("*phone*");
    }

    [Fact]
    public void Update_ChangesNameAndPhone()
    {
        var partner = DeliveryPartner.Create(BranchId, "Old", "111");
        partner.Update("New", "999");
        partner.Name.Should().Be("New");
        partner.Phone.Should().Be("999");
    }

    [Fact]
    public void SetAvailability_False_MarksUnavailable()
    {
        var partner = DeliveryPartner.Create(BranchId, "Rahul", "123");
        partner.SetAvailability(false);
        partner.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void SetAvailability_True_AfterFalse_MarksAvailable()
    {
        var partner = DeliveryPartner.Create(BranchId, "Rahul", "123");
        partner.SetAvailability(false);
        partner.SetAvailability(true);
        partner.IsAvailable.Should().BeTrue();
    }
}
