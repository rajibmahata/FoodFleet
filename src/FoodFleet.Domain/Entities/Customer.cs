using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    private readonly List<CustomerAddress> _addresses = new();
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    private readonly List<Order> _orders = new();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    protected Customer() { }

    public static Customer Create(string name, string email, string phone, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new Customer { Name = name, Email = email.ToLowerInvariant().Trim(), Phone = phone, PasswordHash = passwordHash };
    }

    public void UpdateProfile(string name, string phone)
    {
        Name = name;
        Phone = phone;
        SetUpdated();
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdated();
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdated();
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token && RefreshTokenExpiry.HasValue && RefreshTokenExpiry.Value > DateTime.UtcNow;
}
