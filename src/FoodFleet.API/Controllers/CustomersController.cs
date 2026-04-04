using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Customers;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Customer profile and saved delivery addresses. Customer JWT required.</summary>
[Authorize]
[Produces("application/json")]
public class CustomersController(ICurrentUserService currentUser) : ApiControllerBase
{
    /// <summary>Get the authenticated customer's profile.</summary>
    /// <response code="200">Profile returned.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetCustomerProfileQuery(currentUser.CustomerId!.Value), ct));

    /// <summary>Update the authenticated customer's name and phone number.</summary>
    /// <response code="200">Profile updated.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateCustomerProfileCommand(currentUser.CustomerId!.Value, request), ct));

    /// <summary>List the customer's saved delivery addresses.</summary>
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(IEnumerable<CustomerAddressDto>), 200)]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetCustomerAddressesQuery(currentUser.CustomerId!.Value), ct));

    /// <summary>Add a new delivery address to the customer's profile.</summary>
    /// <response code="201">Address saved.</response>
    [HttpPost("me/addresses")]
    [ProducesResponseType(typeof(CustomerAddressDto), 201)]
    public async Task<IActionResult> AddAddress([FromBody] CreateAddressRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new AddCustomerAddressCommand(currentUser.CustomerId!.Value, request), ct));

    /// <summary>Delete a saved delivery address.</summary>
    /// <response code="200">Address deleted.</response>
    /// <response code="404">Address not found.</response>
    [HttpDelete("me/addresses/{addressId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new DeleteCustomerAddressCommand(currentUser.CustomerId!.Value, addressId), ct));

    /// <summary>Set an address as the customer's default delivery address.</summary>
    /// <response code="200">Default address updated.</response>
    [HttpPatch("me/addresses/{addressId:guid}/default")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SetDefaultAddress(Guid addressId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new SetDefaultAddressCommand(currentUser.CustomerId!.Value, addressId), ct));
}
