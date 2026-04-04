using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using MediatR;

namespace FoodFleet.Application.Features.Customers;

// ── Queries ──────────────────────────────────────────────────
public record GetCustomerProfileQuery(Guid CustomerId) : IRequest<Result<CustomerDto>>;
public record GetCustomerAddressesQuery(Guid CustomerId) : IRequest<Result<IEnumerable<CustomerAddressDto>>>;

// ── Commands ─────────────────────────────────────────────────
public record UpdateCustomerProfileCommand(Guid CustomerId, UpdateProfileRequest Request) : IRequest<Result<CustomerDto>>;
public record AddCustomerAddressCommand(Guid CustomerId, CreateAddressRequest Request) : IRequest<Result<CustomerAddressDto>>;
public record DeleteCustomerAddressCommand(Guid CustomerId, Guid AddressId) : IRequest<Result>;

// ── Handlers ─────────────────────────────────────────────────
public class GetCustomerProfileQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCustomerProfileQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerProfileQuery query, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByIdAsync(query.CustomerId, ct);
        return customer is null
            ? Result<CustomerDto>.NotFound()
            : Result<CustomerDto>.Success(new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt));
    }
}

public class UpdateCustomerProfileCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateCustomerProfileCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(UpdateCustomerProfileCommand cmd, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is null) return Result<CustomerDto>.NotFound();
        customer.UpdateProfile(cmd.Request.Name, cmd.Request.Phone);
        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt));
    }
}

public class GetCustomerAddressesQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCustomerAddressesQuery, Result<IEnumerable<CustomerAddressDto>>>
{
    public async Task<Result<IEnumerable<CustomerAddressDto>>> Handle(GetCustomerAddressesQuery query, CancellationToken ct)
    {
        var addresses = await uow.CustomerAddresses.GetByCustomerAsync(query.CustomerId, ct);
        return Result<IEnumerable<CustomerAddressDto>>.Success(addresses.Select(CustomerMapper.MapAddressDto));
    }
}

public class AddCustomerAddressCommandHandler(IUnitOfWork uow) : IRequestHandler<AddCustomerAddressCommand, Result<CustomerAddressDto>>
{
    public async Task<Result<CustomerAddressDto>> Handle(AddCustomerAddressCommand cmd, CancellationToken ct)
    {
        if (cmd.Request.IsDefault)
            await uow.CustomerAddresses.ClearDefaultAsync(cmd.CustomerId, ct);

        var address = CustomerAddress.Create(cmd.CustomerId, cmd.Request.Label, cmd.Request.FullAddress, cmd.Request.Lat, cmd.Request.Lng, cmd.Request.IsDefault);
        await uow.CustomerAddresses.AddAsync(address, ct);
        await uow.SaveChangesAsync(ct);
        return Result<CustomerAddressDto>.Success(CustomerMapper.MapAddressDto(address), 201);
    }
}

public class DeleteCustomerAddressCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCustomerAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteCustomerAddressCommand cmd, CancellationToken ct)
    {
        var address = await uow.CustomerAddresses.GetByIdAsync(cmd.AddressId, ct);
        if (address is null || address.CustomerId != cmd.CustomerId) return Result.NotFound();
        uow.CustomerAddresses.Remove(address);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class CustomerMapper
{
    internal static CustomerAddressDto MapAddressDto(CustomerAddress a) =>
        new(a.Id, a.Label, a.FullAddress, a.Lat, a.Lng, a.IsDefault);
}

public record SetDefaultAddressCommand(Guid CustomerId, Guid AddressId) : IRequest<Result>;

public class SetDefaultAddressCommandHandler(IUnitOfWork uow) : IRequestHandler<SetDefaultAddressCommand, Result>
{
    public async Task<Result> Handle(SetDefaultAddressCommand cmd, CancellationToken ct)
    {
        var address = await uow.CustomerAddresses.GetByIdAsync(cmd.AddressId, ct);
        if (address is null || address.CustomerId != cmd.CustomerId) return Result.NotFound();

        var allAddresses = await uow.CustomerAddresses.GetByCustomerAsync(cmd.CustomerId, ct);
        foreach (var a in allAddresses.Where(a => a.IsDefault))
            a.UnsetDefault();

        address.SetAsDefault();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
