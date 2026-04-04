using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using MediatR;

namespace FoodFleet.Application.Features.Geo;

public record ValidateDeliveryQuery(Guid BranchId, double DeliveryLat, double DeliveryLng) : IRequest<Result<GeoValidationResponse>>;

public class ValidateDeliveryQueryHandler(IUnitOfWork uow, IGeoService geoService)
    : IRequestHandler<ValidateDeliveryQuery, Result<GeoValidationResponse>>
{
    public async Task<Result<GeoValidationResponse>> Handle(ValidateDeliveryQuery query, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(query.BranchId, ct);
        if (branch is null) return Result<GeoValidationResponse>.NotFound("Branch not found.");

        var distanceKm = geoService.CalculateDistanceKm(branch.Lat, branch.Lng, query.DeliveryLat, query.DeliveryLng);
        var isDeliverable = distanceKm <= branch.DeliveryRadiusKm;

        return Result<GeoValidationResponse>.Success(new GeoValidationResponse(isDeliverable, Math.Round(distanceKm, 2), branch.DeliveryRadiusKm));
    }
}
