using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class MenuItemVariant : BaseEntity
{
    public Guid MenuItemId { get; private set; }
    public string Label { get; private set; } = default!;
    public decimal AdditionalPrice { get; private set; }

    public MenuItem MenuItem { get; private set; } = default!;

    protected MenuItemVariant() { }

    public static MenuItemVariant Create(Guid menuItemId, string label, decimal additionalPrice = 0)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Variant label is required.", nameof(label));
        if (additionalPrice < 0) throw new ArgumentException("Additional price cannot be negative.", nameof(additionalPrice));

        return new MenuItemVariant { MenuItemId = menuItemId, Label = label, AdditionalPrice = additionalPrice };
    }

    public void Update(string label, decimal additionalPrice)
    {
        Label = label;
        AdditionalPrice = additionalPrice;
        SetUpdated();
    }
}
