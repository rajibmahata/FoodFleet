using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodFleet.Infrastructure.Repositories;

public class RestaurantRepository(FoodFleetDbContext ctx) : Repository<Restaurant>(ctx), IRestaurantRepository
{
    public async Task<Restaurant?> GetFirstActiveAsync(CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(r => r.IsActive, ct);
}

public class BranchRepository(FoodFleetDbContext ctx) : Repository<Branch>(ctx), IBranchRepository
{
    public async Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken ct = default) =>
        await DbSet.Where(b => b.IsActive).ToListAsync(ct);

    public async Task<Branch?> GetWithMenuAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(b => b.MenuCategories.Where(c => c.IsActive))
                .ThenInclude(c => c.Items.Where(i => i.IsAvailable))
                    .ThenInclude(i => i.Variants)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Branch?> FindNearestAsync(double lat, double lng, CancellationToken ct = default)
    {
        // Haversine done in memory (acceptable for small number of branches)
        var branches = await DbSet.Where(b => b.IsActive).ToListAsync(ct);
        if (!branches.Any()) return null;

        const double earthRadiusKm = 6371.0;
        return branches
            .Select(b => new
            {
                Branch = b,
                Distance = 2 * earthRadiusKm * Math.Asin(
                    Math.Sqrt(
                        Math.Pow(Math.Sin((ToRad(b.Lat) - ToRad(lat)) / 2), 2) +
                        Math.Cos(ToRad(lat)) * Math.Cos(ToRad(b.Lat)) *
                        Math.Pow(Math.Sin((ToRad(b.Lng) - ToRad(lng)) / 2), 2)
                    ))
            })
            .MinBy(x => x.Distance)?.Branch;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}

public class MenuCategoryRepository(FoodFleetDbContext ctx) : Repository<MenuCategory>(ctx), IMenuCategoryRepository
{
    public async Task<IEnumerable<MenuCategory>> GetByBranchAsync(Guid branchId, CancellationToken ct = default) =>
        await DbSet
            .Where(c => c.BranchId == branchId)
            .Include(c => c.Items).ThenInclude(i => i.Variants)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

    public async Task<MenuCategory?> GetWithItemsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(c => c.Items).ThenInclude(i => i.Variants)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}

public class MenuItemRepository(FoodFleetDbContext ctx) : Repository<MenuItem>(ctx), IMenuItemRepository
{
    public async Task<IEnumerable<MenuItem>> GetByBranchAsync(Guid branchId, bool availableOnly = true, CancellationToken ct = default) =>
        await DbSet
            .Include(i => i.Variants)
            .Where(i => i.Category.BranchId == branchId && (!availableOnly || i.IsAvailable))
            .ToListAsync(ct);

    public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await DbSet
            .Include(i => i.Variants)
            .Where(i => i.CategoryId == categoryId)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);

    public async Task<IEnumerable<MenuItem>> GetLowStockItemsAsync(Guid branchId, int threshold = 5, CancellationToken ct = default) =>
        await DbSet
            .Where(i => i.Category.BranchId == branchId && i.StockCount.HasValue && i.StockCount <= threshold)
            .ToListAsync(ct);

    public async Task<MenuItem?> GetWithVariantsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(i => i.Variants)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
}

public class CustomerRepository(FoodFleetDbContext ctx) : Repository<Customer>(ctx), ICustomerRepository
{
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant().Trim(), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await DbSet.AnyAsync(c => c.Email == email.ToLowerInvariant().Trim(), ct);
}

public class CustomerAddressRepository(FoodFleetDbContext ctx) : Repository<CustomerAddress>(ctx), ICustomerAddressRepository
{
    public async Task<IEnumerable<CustomerAddress>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await DbSet.Where(a => a.CustomerId == customerId).ToListAsync(ct);

    public async Task ClearDefaultAsync(Guid customerId, CancellationToken ct = default)
    {
        var defaults = await DbSet.Where(a => a.CustomerId == customerId && a.IsDefault).ToListAsync(ct);
        foreach (var a in defaults)
        {
            a.UnsetDefault();
            DbSet.Update(a);
        }
    }
}

public class OrderRepository(FoodFleetDbContext ctx) : Repository<Order>(ctx), IOrderRepository
{
    public async Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Include(o => o.Delivery).ThenInclude(d => d!.DeliveryPartner)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Order>> GetByBranchAsync(Guid branchId, string? statusFilter, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = DbSet.Where(o => o.BranchId == branchId);
        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<Domain.Enums.OrderStatus>(statusFilter, true, out var status))
            query = query.Where(o => o.Status == status);
        if (from.HasValue) query = query.Where(o => o.PlacedAt >= from.Value);
        if (to.HasValue)   query = query.Where(o => o.PlacedAt < to.Value);
        return await query.Include(o => o.Items).ThenInclude(i => i.MenuItem).OrderByDescending(o => o.PlacedAt).ToListAsync(ct);
    }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync(CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.Status == Domain.Enums.OrderStatus.Confirmed || o.Status == Domain.Enums.OrderStatus.Preparing)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(ct);
}

public class DeliveryPartnerRepository(FoodFleetDbContext ctx) : Repository<DeliveryPartner>(ctx), IDeliveryPartnerRepository
{
    public async Task<IEnumerable<DeliveryPartner>> GetByBranchAsync(Guid branchId, CancellationToken ct = default) =>
        await DbSet.Where(p => p.BranchId == branchId).ToListAsync(ct);

    public async Task<IEnumerable<DeliveryPartner>> GetAvailableByBranchAsync(Guid branchId, CancellationToken ct = default) =>
        await DbSet.Where(p => p.BranchId == branchId && p.IsAvailable).ToListAsync(ct);
}

public class PaymentRepository(FoodFleetDbContext ctx) : Repository<Payment>(ctx), IPaymentRepository
{
    public async Task<Payment?> GetByOrderAsync(Guid orderId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public async Task<IEnumerable<Payment>> GetByGatewayAsync(string gateway, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet.Where(p => p.Gateway == gateway && p.PaidAt >= from && p.PaidAt < to).ToListAsync(ct);
}

public class SettingsRepository(FoodFleetDbContext ctx) : Repository<RestaurantSettings>(ctx), ISettingsRepository
{
    public async Task<string?> GetValueAsync(Guid restaurantId, string key, CancellationToken ct = default)
    {
        var setting = await DbSet.FirstOrDefaultAsync(s => s.RestaurantId == restaurantId && s.Key == key, ct);
        return setting?.Value;
    }

    public async Task<IEnumerable<RestaurantSettings>> GetAllByRestaurantAsync(Guid restaurantId, CancellationToken ct = default) =>
        await DbSet.Where(s => s.RestaurantId == restaurantId).ToListAsync(ct);

    public async Task UpsertAsync(Guid restaurantId, string key, string value, CancellationToken ct = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(s => s.RestaurantId == restaurantId && s.Key == key, ct);
        if (existing is null)
        {
            var setting = RestaurantSettings.Create(restaurantId, key, value);
            await DbSet.AddAsync(setting, ct);
        }
        else
        {
            existing.UpdateValue(value);
            DbSet.Update(existing);
        }
    }
}

public class NotificationLogRepository(FoodFleetDbContext ctx) : Repository<NotificationLog>(ctx), INotificationLogRepository
{
    public async Task<IEnumerable<NotificationLog>> GetByOrderAsync(Guid orderId, CancellationToken ct = default) =>
        await DbSet.Where(l => l.OrderId == orderId).ToListAsync(ct);
}
