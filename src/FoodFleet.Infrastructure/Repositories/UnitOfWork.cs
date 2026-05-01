using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodFleet.Infrastructure.Repositories;

public class UnitOfWork(FoodFleetDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IRestaurantRepository Restaurants { get; } = new RestaurantRepository(context);
    public IBranchRepository Branches { get; } = new BranchRepository(context);
    public IMenuCategoryRepository MenuCategories { get; } = new MenuCategoryRepository(context);
    public IMenuItemRepository MenuItems { get; } = new MenuItemRepository(context);
    public ICustomerRepository Customers { get; } = new CustomerRepository(context);
    public ICustomerAddressRepository CustomerAddresses { get; } = new CustomerAddressRepository(context);
    public IOrderRepository Orders { get; } = new OrderRepository(context);
    public IDeliveryPartnerRepository DeliveryPartners { get; } = new DeliveryPartnerRepository(context);
    public IPaymentRepository Payments { get; } = new PaymentRepository(context);
    public ISettingsRepository Settings { get; } = new SettingsRepository(context);
    public INotificationLogRepository NotificationLogs { get; } = new NotificationLogRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
