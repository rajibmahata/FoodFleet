using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using MediatR;

namespace FoodFleet.Application.Features.Branches;

// ── Queries ──────────────────────────────────────────────────
public record GetAllBranchesQuery : IRequest<Result<IEnumerable<BranchDto>>>;
public record GetBranchByIdQuery(Guid Id) : IRequest<Result<BranchDto>>;
public record GetNearestBranchQuery(double Lat, double Lng) : IRequest<Result<BranchDto>>;
public record GetBranchMenuQuery(Guid BranchId) : IRequest<Result<BranchMenuResponse>>;

// ── Commands ─────────────────────────────────────────────────
public record CreateBranchCommand(CreateBranchRequest Request) : IRequest<Result<BranchDto>>;
public record UpdateBranchCommand(Guid Id, UpdateBranchRequest Request) : IRequest<Result<BranchDto>>;
public record DeleteBranchCommand(Guid Id) : IRequest<Result>;
public record UpdateDeliveryRadiusCommand(Guid Id, double RadiusKm) : IRequest<Result<BranchDto>>;

// ── Handlers ─────────────────────────────────────────────────
public class GetAllBranchesQueryHandler(IUnitOfWork uow) : IRequestHandler<GetAllBranchesQuery, Result<IEnumerable<BranchDto>>>
{
    public async Task<Result<IEnumerable<BranchDto>>> Handle(GetAllBranchesQuery _, CancellationToken ct)
    {
        var branches = await uow.Branches.GetActiveBranchesAsync(ct);
        return Result<IEnumerable<BranchDto>>.Success(branches.Select(BranchMapper.MapToDto));
    }
}

public class GetBranchByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetBranchByIdQuery, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(GetBranchByIdQuery query, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(query.Id, ct);
        return branch is null ? Result<BranchDto>.NotFound() : Result<BranchDto>.Success(BranchMapper.MapToDto(branch));
    }
}

public class GetNearestBranchQueryHandler(IUnitOfWork uow) : IRequestHandler<GetNearestBranchQuery, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(GetNearestBranchQuery query, CancellationToken ct)
    {
        var branch = await uow.Branches.FindNearestAsync(query.Lat, query.Lng, ct);
        return branch is null ? Result<BranchDto>.NotFound("No active branches found.") : Result<BranchDto>.Success(BranchMapper.MapToDto(branch));
    }
}

public class GetBranchMenuQueryHandler(IUnitOfWork uow) : IRequestHandler<GetBranchMenuQuery, Result<BranchMenuResponse>>
{
    public async Task<Result<BranchMenuResponse>> Handle(GetBranchMenuQuery query, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(query.BranchId, ct);
        if (branch is null) return Result<BranchMenuResponse>.NotFound();

        var categories = await uow.MenuCategories.GetByBranchAsync(query.BranchId, ct);
        var branchDto = BranchMapper.MapToDto(branch);
        var catDtos = categories.Where(c => c.IsActive).Select(c => new MenuCategoryDto(
            c.Id, c.BranchId, c.Name, c.DisplayOrder, c.IsActive,
            c.Items.Where(i => i.IsAvailable).Select(i => new MenuItemDto(
                i.Id, i.CategoryId, i.Name, i.Description, i.Price, i.ImageUrl, i.IsAvailable, i.StockCount,
                i.Variants.Select(v => new MenuItemVariantDto(v.Id, v.Label, v.AdditionalPrice))))
        ));
        return Result<BranchMenuResponse>.Success(new BranchMenuResponse(branchDto, catDtos));
    }
}

public class CreateBranchCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(CreateBranchCommand cmd, CancellationToken ct)
    {
        var restaurant = await uow.Restaurants.GetFirstActiveAsync(ct);
        if (restaurant is null) return Result<BranchDto>.Failure("No active restaurant found.", 400);
        var branch = Branch.Create(restaurant.Id, cmd.Request.Name, cmd.Request.Lat, cmd.Request.Lng, cmd.Request.Address, cmd.Request.DeliveryRadiusKm);
        await uow.Branches.AddAsync(branch, ct);
        await uow.SaveChangesAsync(ct);
        return Result<BranchDto>.Success(BranchMapper.MapToDto(branch), 201);
    }
}

public class UpdateBranchCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateBranchCommand, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(UpdateBranchCommand cmd, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(cmd.Id, ct);
        if (branch is null) return Result<BranchDto>.NotFound();
        branch.Update(cmd.Request.Name, cmd.Request.Lat, cmd.Request.Lng, cmd.Request.Address, cmd.Request.DeliveryRadiusKm);
        uow.Branches.Update(branch);
        await uow.SaveChangesAsync(ct);
        return Result<BranchDto>.Success(BranchMapper.MapToDto(branch));
    }
}

public class DeleteBranchCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteBranchCommand, Result>
{
    public async Task<Result> Handle(DeleteBranchCommand cmd, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(cmd.Id, ct);  
        if (branch is null) return Result.NotFound();
        branch.Deactivate();
        uow.Branches.Update(branch);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class UpdateDeliveryRadiusCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateDeliveryRadiusCommand, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(UpdateDeliveryRadiusCommand cmd, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(cmd.Id, ct);
        if (branch is null) return Result<BranchDto>.NotFound();
        branch.UpdateDeliveryRadius(cmd.RadiusKm);
        uow.Branches.Update(branch);
        await uow.SaveChangesAsync(ct);
        return Result<BranchDto>.Success(BranchMapper.MapToDto(branch));
    }
}

internal static class BranchMapper
{
    internal static BranchDto MapToDto(Branch b) => new(b.Id, b.RestaurantId, b.Name, b.Lat, b.Lng, b.Address, b.DeliveryRadiusKm, b.IsActive);
}
