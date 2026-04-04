using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class Restaurant : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? LogoUrl { get; private set; }
    public string ContactEmail { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private readonly List<Branch> _branches = new();
    public IReadOnlyCollection<Branch> Branches => _branches.AsReadOnly();

    private readonly List<RestaurantSettings> _settings = new();
    public IReadOnlyCollection<RestaurantSettings> Settings => _settings.AsReadOnly();

    protected Restaurant() { }

    public static Restaurant Create(string name, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Restaurant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(contactEmail)) throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        return new Restaurant { Name = name, ContactEmail = contactEmail };
    }

    public void Update(string name, string contactEmail, string? logoUrl)
    {
        Name = name;
        ContactEmail = contactEmail;
        LogoUrl = logoUrl;
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
    public void Activate()   { IsActive = true;  SetUpdated(); }
}
