using FoodFleet.Domain.Entities;

namespace FoodFleet.Domain.Interfaces.Repositories;

public interface IRestaurantRepository : IRepository<Restaurant>
{
    Task<Restaurant?> GetFirstActiveAsync(CancellationToken ct = default);
}

public interface IBranchRepository : IRepository<Branch>
{
    Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken ct = default);
    Task<Branch?> GetWithMenuAsync(Guid id, CancellationToken ct = default);
    Task<Branch?> FindNearestAsync(double lat, double lng, CancellationToken ct = default);
}

public interface IMenuCategoryRepository : IRepository<MenuCategory>
{
    Task<IEnumerable<MenuCategory>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<MenuCategory?> GetWithItemsAsync(Guid id, CancellationToken ct = default);
}

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IEnumerable<MenuItem>> GetByBranchAsync(Guid branchId, bool availableOnly = true, CancellationToken ct = default);
    Task<IEnumerable<MenuItem>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<IEnumerable<MenuItem>> GetLowStockItemsAsync(Guid branchId, int threshold = 5, CancellationToken ct = default);
    Task<MenuItem?> GetWithVariantsAsync(Guid id, CancellationToken ct = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}

public interface ICustomerAddressRepository : IRepository<CustomerAddress>
{
    Task<IEnumerable<CustomerAddress>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task ClearDefaultAsync(Guid customerId, CancellationToken ct = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByBranchAsync(Guid branchId, string? statusFilter = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetPendingOrdersAsync(CancellationToken ct = default);
}

public interface IDeliveryPartnerRepository : IRepository<DeliveryPartner>
{
    Task<IEnumerable<DeliveryPartner>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<IEnumerable<DeliveryPartner>> GetAvailableByBranchAsync(Guid branchId, CancellationToken ct = default);
}

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetByGatewayAsync(string gateway, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface ISettingsRepository : IRepository<RestaurantSettings>
{
    Task<string?> GetValueAsync(Guid restaurantId, string key, CancellationToken ct = default);
    Task<IEnumerable<RestaurantSettings>> GetAllByRestaurantAsync(Guid restaurantId, CancellationToken ct = default);
    Task UpsertAsync(Guid restaurantId, string key, string value, CancellationToken ct = default);
}

public interface INotificationLogRepository : IRepository<NotificationLog>
{
    Task<IEnumerable<NotificationLog>> GetByOrderAsync(Guid orderId, CancellationToken ct = default);
}
