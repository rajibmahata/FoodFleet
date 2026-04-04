using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Customers;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class GetCustomerProfileQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();

    public GetCustomerProfileQueryHandlerTests()
    {
        _uow.Setup(u => u.Customers).Returns(_customerRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsProfile()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "9876543210", "hash");
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await new GetCustomerProfileQueryHandler(_uow.Object)
            .Handle(new GetCustomerProfileQuery(customer.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Alice");
        result.Data.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task Handle_CustomerNotFound_Returns404()
    {
        _customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await new GetCustomerProfileQueryHandler(_uow.Object)
            .Handle(new GetCustomerProfileQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class UpdateCustomerProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();

    public UpdateCustomerProfileCommandHandlerTests()
    {
        _uow.Setup(u => u.Customers).Returns(_customerRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _customerRepo.Setup(r => r.Update(It.IsAny<Customer>()));
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedProfile()
    {
        var customer = Customer.Create("Old Name", "old@test.com", "111", "hash");
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await new UpdateCustomerProfileCommandHandler(_uow.Object)
            .Handle(new UpdateCustomerProfileCommand(customer.Id, new UpdateProfileRequest("New Name", "999")),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
        result.Data.Phone.Should().Be("999");
    }

    [Fact]
    public async Task Handle_CustomerNotFound_Returns404()
    {
        _customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await new UpdateCustomerProfileCommandHandler(_uow.Object)
            .Handle(new UpdateCustomerProfileCommand(Guid.NewGuid(), new UpdateProfileRequest("N", "1")),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class GetCustomerAddressesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerAddressRepository> _addressRepo = new();

    public GetCustomerAddressesQueryHandlerTests()
    {
        _uow.Setup(u => u.CustomerAddresses).Returns(_addressRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAddressesForCustomer()
    {
        var customerId = Guid.NewGuid();
        var addresses = new List<CustomerAddress>
        {
            CustomerAddress.Create(customerId, "Home", "1 Home St", 12.9, 77.6, true),
            CustomerAddress.Create(customerId, "Work", "2 Work Ave", 12.8, 77.5, false),
        };
        _addressRepo.Setup(r => r.GetByCustomerAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        var result = await new GetCustomerAddressesQueryHandler(_uow.Object)
            .Handle(new GetCustomerAddressesQuery(customerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }
}

public class AddCustomerAddressCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerAddressRepository> _addressRepo = new();

    public AddCustomerAddressCommandHandlerTests()
    {
        _uow.Setup(u => u.CustomerAddresses).Returns(_addressRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _addressRepo.Setup(r => r.AddAsync(It.IsAny<CustomerAddress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _addressRepo.Setup(r => r.ClearDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_DefaultAddress_ClearsOthersFirst()
    {
        var customerId = Guid.NewGuid();
        var request = new CreateAddressRequest("Home", "1 Home St", 12.9, 77.6, true);

        var result = await new AddCustomerAddressCommandHandler(_uow.Object)
            .Handle(new AddCustomerAddressCommand(customerId, request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _addressRepo.Verify(r => r.ClearDefaultAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonDefaultAddress_DoesNotClearOthers()
    {
        var customerId = Guid.NewGuid();
        var request = new CreateAddressRequest("Work", "2 Work St", 12.8, 77.5, false);

        await new AddCustomerAddressCommandHandler(_uow.Object)
            .Handle(new AddCustomerAddressCommand(customerId, request), CancellationToken.None);

        _addressRepo.Verify(r => r.ClearDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class DeleteCustomerAddressCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerAddressRepository> _addressRepo = new();

    public DeleteCustomerAddressCommandHandlerTests()
    {
        _uow.Setup(u => u.CustomerAddresses).Returns(_addressRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _addressRepo.Setup(r => r.Remove(It.IsAny<CustomerAddress>()));
    }

    [Fact]
    public async Task Handle_OwnAddress_Deletes()
    {
        var customerId = Guid.NewGuid();
        var address = CustomerAddress.Create(customerId, "Home", "1 Home St", 0, 0, true);
        _addressRepo.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await new DeleteCustomerAddressCommandHandler(_uow.Object)
            .Handle(new DeleteCustomerAddressCommand(customerId, address.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _addressRepo.Verify(r => r.Remove(address), Times.Once);
    }

    [Fact]
    public async Task Handle_AddressNotFound_Returns404()
    {
        _addressRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerAddress?)null);

        var result = await new DeleteCustomerAddressCommandHandler(_uow.Object)
            .Handle(new DeleteCustomerAddressCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_AddressBelongsToOtherCustomer_Returns404()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var address = CustomerAddress.Create(ownerId, "Home", "1 Home St", 0, 0, true);
        _addressRepo.Setup(r => r.GetByIdAsync(address.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await new DeleteCustomerAddressCommandHandler(_uow.Object)
            .Handle(new DeleteCustomerAddressCommand(requesterId, address.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
