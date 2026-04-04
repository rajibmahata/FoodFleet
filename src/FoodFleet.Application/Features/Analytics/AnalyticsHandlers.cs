using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using MediatR;

namespace FoodFleet.Application.Features.Analytics;

public record GetDashboardSummaryQuery(Guid? BranchId = null) : IRequest<Result<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler(IUnitOfWork uow) : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery query, CancellationToken ct)
    {
        var branches = await uow.Branches.GetActiveBranchesAsync(ct);
        var activeBranches = branches.Count();

        var today = DateTime.UtcNow.Date;
        var allTodayOrders = new List<Domain.Entities.Order>();
        var lowStockCount = 0;

        foreach (var branch in branches)
        {
            var branchOrders = await uow.Orders.GetByBranchAsync(branch.Id, null, today, today.AddDays(1), ct);
            allTodayOrders.AddRange(branchOrders);

            var lowStock = await uow.MenuItems.GetLowStockItemsAsync(branch.Id, 5, ct);
            lowStockCount += lowStock.Count();
        }

        var pendingOrders = await uow.Orders.GetPendingOrdersAsync(ct);

        var summary = new DashboardSummaryDto(
            TodaysOrders: allTodayOrders.Count,
            TodaysRevenue: allTodayOrders.Sum(o => o.TotalAmount),
            PendingOrders: pendingOrders.Count(),
            ActiveBranches: activeBranches,
            LowStockItems: lowStockCount
        );

        return Result<DashboardSummaryDto>.Success(summary);
    }
}
