using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class Branch : BaseEntity
{
    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; } = default!;
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string Address { get; private set; } = default!;
    public double DeliveryRadiusKm { get; private set; } = 3.0;
    public bool IsActive { get; private set; } = true;

    public Restaurant Restaurant { get; private set; } = default!;

    private readonly List<MenuCategory> _menuCategories = new();
    public IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();

    private readonly List<DeliveryPartner> _deliveryPartners = new();
    public IReadOnlyCollection<DeliveryPartner> DeliveryPartners => _deliveryPartners.AsReadOnly();

    protected Branch() { }

    public static Branch Create(Guid restaurantId, string name, double lat, double lng, string address, double deliveryRadiusKm = 3.0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Branch name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Branch address is required.", nameof(address));
        return new Branch
        {
            RestaurantId = restaurantId,
            Name = name,
            Lat = lat,
            Lng = lng,
            Address = address,
            DeliveryRadiusKm = deliveryRadiusKm
        };
    }

    public void Update(string name, double lat, double lng, string address, double deliveryRadiusKm)
    {
        Name = name;
        Lat = lat;
        Lng = lng;
        Address = address;
        DeliveryRadiusKm = deliveryRadiusKm;
        SetUpdated();
    }

    public void UpdateDeliveryRadius(double radiusKm)
    {
        if (radiusKm <= 0) throw new ArgumentException("Delivery radius must be positive.", nameof(radiusKm));
        DeliveryRadiusKm = radiusKm;
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
    public void Activate()   { IsActive = true;  SetUpdated(); }
}
