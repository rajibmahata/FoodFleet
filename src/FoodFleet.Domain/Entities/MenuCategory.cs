using FoodFleet.Domain.Common;

namespace FoodFleet.Domain.Entities;

public class MenuCategory : BaseEntity
{
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Branch Branch { get; private set; } = default!;

    private readonly List<MenuItem> _items = new();
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    protected MenuCategory() { }

    public static MenuCategory Create(Guid branchId, string name, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        return new MenuCategory { BranchId = branchId, Name = name, DisplayOrder = displayOrder };
    }

    public void Update(string name, int displayOrder)
    {
        Name = name;
        DisplayOrder = displayOrder;
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
    public void Activate()   { IsActive = true;  SetUpdated(); }
}
