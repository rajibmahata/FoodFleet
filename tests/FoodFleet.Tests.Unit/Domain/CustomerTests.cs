using FoodFleet.Domain.Entities;
using FluentAssertions;

namespace FoodFleet.Tests.Unit.Domain;

public class CustomerTests
{
    // ── Create ────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsCustomer()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "9876543210", "hashedpwd");
        customer.Name.Should().Be("Alice");
        customer.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public void Create_EmailIsLowercased()
    {
        var customer = Customer.Create("Bob", "BOB@TEST.COM", "1234567890", "hash");
        customer.Email.Should().Be("bob@test.com");
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => Customer.Create("", "x@x.com", "123", "hash");
        act.Should().Throw<ArgumentException>().WithMessage("*Name is required*");
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        var act = () => Customer.Create("Name", "", "123", "hash");
        act.Should().Throw<ArgumentException>().WithMessage("*Email is required*");
    }

    [Fact]
    public void Create_EmptyPhone_Throws()
    {
        var act = () => Customer.Create("Name", "x@x.com", "", "hash");
        act.Should().Throw<ArgumentException>().WithMessage("*Phone is required*");
    }

    // ── UpdateProfile ─────────────────────────────────────────

    [Fact]
    public void UpdateProfile_UpdatesNameAndPhone()
    {
        var customer = Customer.Create("Old", "x@x.com", "111", "hash");
        customer.UpdateProfile("New", "999");
        customer.Name.Should().Be("New");
        customer.Phone.Should().Be("999");
    }

    // ── RefreshToken ──────────────────────────────────────────

    [Fact]
    public void SetRefreshToken_StoresTokenAndExpiry()
    {
        var customer = Customer.Create("Alice", "a@test.com", "123", "hash");
        var expiry = DateTime.UtcNow.AddDays(7);
        customer.SetRefreshToken("mytoken", expiry);
        customer.RefreshToken.Should().Be("mytoken");
        customer.RefreshTokenExpiry.Should().BeCloseTo(expiry, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsRefreshTokenValid_MatchingNonExpiredToken_ReturnsTrue()
    {
        var customer = Customer.Create("Alice", "a@test.com", "123", "hash");
        customer.SetRefreshToken("validtoken", DateTime.UtcNow.AddDays(7));
        customer.IsRefreshTokenValid("validtoken").Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_WrongToken_ReturnsFalse()
    {
        var customer = Customer.Create("Alice", "a@test.com", "123", "hash");
        customer.SetRefreshToken("validtoken", DateTime.UtcNow.AddDays(7));
        customer.IsRefreshTokenValid("wrongtoken").Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_ExpiredToken_ReturnsFalse()
    {
        var customer = Customer.Create("Alice", "a@test.com", "123", "hash");
        customer.SetRefreshToken("expiredtoken", DateTime.UtcNow.AddDays(-1));
        customer.IsRefreshTokenValid("expiredtoken").Should().BeFalse();
    }
}
