using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class DeliveryPartner : BaseEntity
{
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public bool IsAvailable { get; private set; } = true;
    public double? CurrentLat { get; private set; }   // reserved for Phase 2
    public double? CurrentLng { get; private set; }   // reserved for Phase 2

    public Branch Branch { get; private set; } = default!;

    protected DeliveryPartner() { }

    public static DeliveryPartner Create(Guid branchId, string name, string phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        return new DeliveryPartner { BranchId = branchId, Name = name, Phone = phone };
    }

    public void Update(string name, string phone)
    {
        Name = name;
        Phone = phone;
        SetUpdated();
    }

    public void SetAvailability(bool isAvailable) { IsAvailable = isAvailable; SetUpdated(); }

    public void UpdateLocation(double lat, double lng)
    {
        CurrentLat = lat;
        CurrentLng = lng;
        SetUpdated();
    }
}
