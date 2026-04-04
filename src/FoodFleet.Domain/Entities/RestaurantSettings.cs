using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class RestaurantSettings : BaseEntity
{
    public Guid RestaurantId { get; private set; }
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;

    public Restaurant Restaurant { get; private set; } = default!;

    protected RestaurantSettings() { }

    public static RestaurantSettings Create(Guid restaurantId, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        return new RestaurantSettings { RestaurantId = restaurantId, Key = key, Value = value ?? string.Empty };
    }

    public void UpdateValue(string value)
    {
        Value = value ?? string.Empty;
        SetUpdated();
    }
}
