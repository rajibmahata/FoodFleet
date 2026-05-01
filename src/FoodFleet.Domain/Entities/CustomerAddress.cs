using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class CustomerAddress : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Label { get; private set; } = default!;
    public string FullAddress { get; private set; } = default!;
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public bool IsDefault { get; private set; }

    public Customer Customer { get; private set; } = default!;

    protected CustomerAddress() { }

    public static CustomerAddress Create(Guid customerId, string label, string fullAddress, double lat, double lng, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label is required.", nameof(label));
        if (string.IsNullOrWhiteSpace(fullAddress)) throw new ArgumentException("Full address is required.", nameof(fullAddress));

        return new CustomerAddress
        {
            CustomerId = customerId,
            Label = label,
            FullAddress = fullAddress,
            Lat = lat,
            Lng = lng,
            IsDefault = isDefault
        };
    }

    public void Update(string label, string fullAddress, double lat, double lng) 
    {
        Label = label;
        FullAddress = fullAddress;
        Lat = lat;
        Lng = lng;
        SetUpdated();
    }

    public void SetAsDefault() { IsDefault = true; SetUpdated(); }
    public void UnsetDefault() { IsDefault = false; SetUpdated(); }
}
