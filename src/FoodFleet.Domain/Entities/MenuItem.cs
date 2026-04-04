using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class MenuItem : BaseEntity
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public int? StockCount { get; private set; }  // null = unlimited

    public MenuCategory Category { get; private set; } = default!;

    private readonly List<MenuItemVariant> _variants = new();
    public IReadOnlyCollection<MenuItemVariant> Variants => _variants.AsReadOnly();

    protected MenuItem() { }

    public static MenuItem Create(Guid categoryId, string name, decimal price, string? description = null, string? imageUrl = null, int? stockCount = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (price < 0) throw new ArgumentException("Price cannot be negative.", nameof(price));

        return new MenuItem
        {
            CategoryId = categoryId,
            Name = name,
            Price = price,
            Description = description,
            ImageUrl = imageUrl,
            StockCount = stockCount
        };
    }

    public void Update(string name, string? description, decimal price, string? imageUrl)
    {
        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        SetUpdated();
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        SetUpdated();
    }

    public void UpdateStock(int? stockCount)
    {
        StockCount = stockCount;
        if (stockCount.HasValue && stockCount.Value <= 0)
            IsAvailable = false;
        SetUpdated();
    }

    public void DecrementStock()
    {
        if (StockCount.HasValue)
        {
            StockCount = Math.Max(0, StockCount.Value - 1);
            if (StockCount.Value == 0)
                IsAvailable = false;
            SetUpdated();
        }
    }
}
