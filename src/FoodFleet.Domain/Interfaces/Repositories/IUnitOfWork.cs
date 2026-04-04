namespace FoodFleet.Domain.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRestaurantRepository Restaurants { get; }
    IBranchRepository Branches { get; }
    IMenuCategoryRepository MenuCategories { get; }
    IMenuItemRepository MenuItems { get; }
    ICustomerRepository Customers { get; }
    ICustomerAddressRepository CustomerAddresses { get; }
    IOrderRepository Orders { get; }
    IDeliveryPartnerRepository DeliveryPartners { get; }
    IPaymentRepository Payments { get; }
    ISettingsRepository Settings { get; }
    INotificationLogRepository NotificationLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
