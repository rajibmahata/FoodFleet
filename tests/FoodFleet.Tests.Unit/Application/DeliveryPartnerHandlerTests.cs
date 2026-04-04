using FoodFleet.Application.Features.DeliveryPartners;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class GetDeliveryPartnersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDeliveryPartnerRepository> _partnerRepo = new();

    public GetDeliveryPartnersQueryHandlerTests()
    {
        _uow.Setup(u => u.DeliveryPartners).Returns(_partnerRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPartnersForBranch()
    {
        var branchId = Guid.NewGuid();
        var partners = new List<DeliveryPartner>
        {
            DeliveryPartner.Create(branchId, "Raj", "9001"),
            DeliveryPartner.Create(branchId, "Priya", "9002"),
        };
        _partnerRepo.Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partners);

        var result = await new GetDeliveryPartnersQueryHandler(_uow.Object)
            .Handle(new GetDeliveryPartnersQuery(branchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoBranchPartners_ReturnsEmptyList()
    {
        _partnerRepo.Setup(r => r.GetByBranchAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryPartner>());

        var result = await new GetDeliveryPartnersQueryHandler(_uow.Object)
            .Handle(new GetDeliveryPartnersQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }
}

public class CreateDeliveryPartnerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDeliveryPartnerRepository> _partnerRepo = new();

    public CreateDeliveryPartnerCommandHandlerTests()
    {
        _uow.Setup(u => u.DeliveryPartners).Returns(_partnerRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _partnerRepo.Setup(r => r.AddAsync(It.IsAny<DeliveryPartner>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ValidRequest_Returns201WithPartner()
    {
        var branchId = Guid.NewGuid();
        var request = new CreateDeliveryPartnerRequest(branchId, "Kumar", "9876543210");

        var result = await new CreateDeliveryPartnerCommandHandler(_uow.Object)
            .Handle(new CreateDeliveryPartnerCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("Kumar");
        result.Data.Phone.Should().Be("9876543210");
        _partnerRepo.Verify(r => r.AddAsync(It.IsAny<DeliveryPartner>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UpdateDeliveryPartnerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDeliveryPartnerRepository> _partnerRepo = new();

    public UpdateDeliveryPartnerCommandHandlerTests()
    {
        _uow.Setup(u => u.DeliveryPartners).Returns(_partnerRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _partnerRepo.Setup(r => r.Update(It.IsAny<DeliveryPartner>()));
    }

    [Fact]
    public async Task Handle_ExistingPartner_UpdatesAndReturns()
    {
        var partner = DeliveryPartner.Create(Guid.NewGuid(), "Old Name", "111");
        _partnerRepo.Setup(r => r.GetByIdAsync(partner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partner);

        var result = await new UpdateDeliveryPartnerCommandHandler(_uow.Object)
            .Handle(new UpdateDeliveryPartnerCommand(partner.Id,
                new UpdateDeliveryPartnerRequest("New Name", "222")),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
        result.Data.Phone.Should().Be("222");
    }

    [Fact]
    public async Task Handle_PartnerNotFound_Returns404()
    {
        _partnerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryPartner?)null);

        var result = await new UpdateDeliveryPartnerCommandHandler(_uow.Object)
            .Handle(new UpdateDeliveryPartnerCommand(Guid.NewGuid(),
                new UpdateDeliveryPartnerRequest("N", "0")),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class DeleteDeliveryPartnerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDeliveryPartnerRepository> _partnerRepo = new();

    public DeleteDeliveryPartnerCommandHandlerTests()
    {
        _uow.Setup(u => u.DeliveryPartners).Returns(_partnerRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _partnerRepo.Setup(r => r.Remove(It.IsAny<DeliveryPartner>()));
    }

    [Fact]
    public async Task Handle_ExistingPartner_Removes()
    {
        var partner = DeliveryPartner.Create(Guid.NewGuid(), "Sam", "555");
        _partnerRepo.Setup(r => r.GetByIdAsync(partner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partner);

        var result = await new DeleteDeliveryPartnerCommandHandler(_uow.Object)
            .Handle(new DeleteDeliveryPartnerCommand(partner.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _partnerRepo.Verify(r => r.Remove(partner), Times.Once);
    }

    [Fact]
    public async Task Handle_PartnerNotFound_Returns404()
    {
        _partnerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryPartner?)null);

        var result = await new DeleteDeliveryPartnerCommandHandler(_uow.Object)
            .Handle(new DeleteDeliveryPartnerCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class ToggleDeliveryPartnerAvailabilityCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDeliveryPartnerRepository> _partnerRepo = new();

    public ToggleDeliveryPartnerAvailabilityCommandHandlerTests()
    {
        _uow.Setup(u => u.DeliveryPartners).Returns(_partnerRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _partnerRepo.Setup(r => r.Update(It.IsAny<DeliveryPartner>()));
    }

    [Fact]
    public async Task Handle_SetAvailableTrue_PartnerBecomesAvailable()
    {
        var partner = DeliveryPartner.Create(Guid.NewGuid(), "Arjun", "777");
        partner.SetAvailability(false);
        _partnerRepo.Setup(r => r.GetByIdAsync(partner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partner);

        var result = await new ToggleDeliveryPartnerAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleDeliveryPartnerAvailabilityCommand(partner.Id, true),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetAvailableFalse_PartnerBecomesUnavailable()
    {
        var partner = DeliveryPartner.Create(Guid.NewGuid(), "Arjun", "777");
        _partnerRepo.Setup(r => r.GetByIdAsync(partner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(partner);

        var result = await new ToggleDeliveryPartnerAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleDeliveryPartnerAvailabilityCommand(partner.Id, false),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PartnerNotFound_Returns404()
    {
        _partnerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryPartner?)null);

        var result = await new ToggleDeliveryPartnerAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleDeliveryPartnerAvailabilityCommand(Guid.NewGuid(), true),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
