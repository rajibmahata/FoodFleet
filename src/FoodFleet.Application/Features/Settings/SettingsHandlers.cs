using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using MediatR;

namespace FoodFleet.Application.Features.Settings;

public record GetSettingsQuery(Guid RestaurantId) : IRequest<Result<IEnumerable<SettingDto>>>;
public record GetSettingQuery(Guid RestaurantId, string Key) : IRequest<Result<SettingDto>>;
public record UpsertSettingsCommand(Guid RestaurantId, List<UpsertSettingRequest> Settings) : IRequest<Result>;
public record GetRestaurantInfoQuery : IRequest<Result<RestaurantInfoDto>>;

// Parameter-less variants that auto-resolve the single active restaurant (Phase 1 single-restaurant)
public record GetActiveRestaurantSettingsQuery : IRequest<Result<IEnumerable<SettingDto>>>;
public record UpsertActiveRestaurantSettingsCommand(List<UpsertSettingRequest> Settings) : IRequest<Result>;

public class GetSettingsQueryHandler(IUnitOfWork uow) : IRequestHandler<GetSettingsQuery, Result<IEnumerable<SettingDto>>>
{
    public async Task<Result<IEnumerable<SettingDto>>> Handle(GetSettingsQuery query, CancellationToken ct)
    {
        var settings = await uow.Settings.GetAllByRestaurantAsync(query.RestaurantId, ct);
        return Result<IEnumerable<SettingDto>>.Success(settings.Select(s => new SettingDto(s.Key, s.Value)));
    }
}

public class GetActiveRestaurantSettingsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetActiveRestaurantSettingsQuery, Result<IEnumerable<SettingDto>>>
{
    public async Task<Result<IEnumerable<SettingDto>>> Handle(GetActiveRestaurantSettingsQuery _, CancellationToken ct)
    {
        var restaurant = await uow.Restaurants.GetFirstActiveAsync(ct);
        if (restaurant is null) return Result<IEnumerable<SettingDto>>.NotFound("No active restaurant found.");
        var settings = await uow.Settings.GetAllByRestaurantAsync(restaurant.Id, ct);
        return Result<IEnumerable<SettingDto>>.Success(settings.Select(s => new SettingDto(s.Key, s.Value)));
    }
}

public class UpsertSettingsCommandHandler(IUnitOfWork uow) : IRequestHandler<UpsertSettingsCommand, Result>
{
    public async Task<Result> Handle(UpsertSettingsCommand cmd, CancellationToken ct)
    {
        foreach (var s in cmd.Settings)
            await uow.Settings.UpsertAsync(cmd.RestaurantId, s.Key, s.Value, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class UpsertActiveRestaurantSettingsCommandHandler(IUnitOfWork uow)
    : IRequestHandler<UpsertActiveRestaurantSettingsCommand, Result>
{
    public async Task<Result> Handle(UpsertActiveRestaurantSettingsCommand cmd, CancellationToken ct)
    {
        var restaurant = await uow.Restaurants.GetFirstActiveAsync(ct);
        if (restaurant is null) return Result.NotFound("No active restaurant found.");
        foreach (var s in cmd.Settings)
            await uow.Settings.UpsertAsync(restaurant.Id, s.Key, s.Value, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetRestaurantInfoQueryHandler(IUnitOfWork uow) : IRequestHandler<GetRestaurantInfoQuery, Result<RestaurantInfoDto>>
{
    public async Task<Result<RestaurantInfoDto>> Handle(GetRestaurantInfoQuery _, CancellationToken ct)
    {
        var restaurant = await uow.Restaurants.GetFirstActiveAsync(ct);
        return restaurant is null
            ? Result<RestaurantInfoDto>.NotFound()
            : Result<RestaurantInfoDto>.Success(new RestaurantInfoDto(restaurant.Id, restaurant.Name, restaurant.LogoUrl, restaurant.ContactEmail));
    }
}
