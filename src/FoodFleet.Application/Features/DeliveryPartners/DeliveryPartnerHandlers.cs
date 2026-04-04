using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using MediatR;

namespace FoodFleet.Application.Features.DeliveryPartners;

public record GetDeliveryPartnersQuery(Guid BranchId) : IRequest<Result<IEnumerable<DeliveryPartnerDto>>>;
public record CreateDeliveryPartnerCommand(CreateDeliveryPartnerRequest Request) : IRequest<Result<DeliveryPartnerDto>>;
public record UpdateDeliveryPartnerCommand(Guid Id, UpdateDeliveryPartnerRequest Request) : IRequest<Result<DeliveryPartnerDto>>;
public record DeleteDeliveryPartnerCommand(Guid Id) : IRequest<Result>;
public record ToggleDeliveryPartnerAvailabilityCommand(Guid Id, bool IsAvailable) : IRequest<Result<DeliveryPartnerDto>>;

public class GetDeliveryPartnersQueryHandler(IUnitOfWork uow) : IRequestHandler<GetDeliveryPartnersQuery, Result<IEnumerable<DeliveryPartnerDto>>>
{
    public async Task<Result<IEnumerable<DeliveryPartnerDto>>> Handle(GetDeliveryPartnersQuery query, CancellationToken ct)
    {
        var partners = await uow.DeliveryPartners.GetByBranchAsync(query.BranchId, ct);
        return Result<IEnumerable<DeliveryPartnerDto>>.Success(partners.Select(DeliveryPartnerMapper.MapDto));
    }
}

public class CreateDeliveryPartnerCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateDeliveryPartnerCommand, Result<DeliveryPartnerDto>>
{
    public async Task<Result<DeliveryPartnerDto>> Handle(CreateDeliveryPartnerCommand cmd, CancellationToken ct)
    {
        var partner = DeliveryPartner.Create(cmd.Request.BranchId, cmd.Request.Name, cmd.Request.Phone);
        await uow.DeliveryPartners.AddAsync(partner, ct);
        await uow.SaveChangesAsync(ct);
        return Result<DeliveryPartnerDto>.Success(DeliveryPartnerMapper.MapDto(partner), 201);
    }
}

public class UpdateDeliveryPartnerCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateDeliveryPartnerCommand, Result<DeliveryPartnerDto>>
{
    public async Task<Result<DeliveryPartnerDto>> Handle(UpdateDeliveryPartnerCommand cmd, CancellationToken ct)
    {
        var partner = await uow.DeliveryPartners.GetByIdAsync(cmd.Id, ct);
        if (partner is null) return Result<DeliveryPartnerDto>.NotFound();
        partner.Update(cmd.Request.Name, cmd.Request.Phone);
        uow.DeliveryPartners.Update(partner);
        await uow.SaveChangesAsync(ct);
        return Result<DeliveryPartnerDto>.Success(DeliveryPartnerMapper.MapDto(partner));
    }
}

public class DeleteDeliveryPartnerCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteDeliveryPartnerCommand, Result>
{
    public async Task<Result> Handle(DeleteDeliveryPartnerCommand cmd, CancellationToken ct)
    {
        var partner = await uow.DeliveryPartners.GetByIdAsync(cmd.Id, ct);
        if (partner is null) return Result.NotFound();
        uow.DeliveryPartners.Remove(partner);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ToggleDeliveryPartnerAvailabilityCommandHandler(IUnitOfWork uow) : IRequestHandler<ToggleDeliveryPartnerAvailabilityCommand, Result<DeliveryPartnerDto>>
{
    public async Task<Result<DeliveryPartnerDto>> Handle(ToggleDeliveryPartnerAvailabilityCommand cmd, CancellationToken ct)
    {
        var partner = await uow.DeliveryPartners.GetByIdAsync(cmd.Id, ct);
        if (partner is null) return Result<DeliveryPartnerDto>.NotFound();
        partner.SetAvailability(cmd.IsAvailable);
        uow.DeliveryPartners.Update(partner);
        await uow.SaveChangesAsync(ct);
        return Result<DeliveryPartnerDto>.Success(DeliveryPartnerMapper.MapDto(partner));
    }
}

internal static class DeliveryPartnerMapper
{
    internal static DeliveryPartnerDto MapDto(DeliveryPartner p) => new(p.Id, p.BranchId, p.Name, p.Phone, p.IsAvailable);
}
