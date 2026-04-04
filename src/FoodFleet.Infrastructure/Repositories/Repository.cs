using FoodFleet.Domain.Common;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodFleet.Infrastructure.Repositories;

public class Repository<T>(FoodFleetDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly FoodFleetDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await DbSet.AddAsync(entity, ct);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
