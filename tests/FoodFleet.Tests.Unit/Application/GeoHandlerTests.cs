using FoodFleet.Application.Features.Geo;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class ValidateDeliveryQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IGeoService> _geoService = new();

    public ValidateDeliveryQueryHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
    }

    private ValidateDeliveryQueryHandler CreateHandler() =>
        new(_uow.Object, _geoService.Object);

    [Fact]
    public async Task Handle_AddressWithinRadius_ReturnsDeliverable()
    {
        var branch = Branch.Create(Guid.NewGuid(), "City", 12.9, 77.6, "Addr", 10.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(12.9, 77.6, 12.91, 77.61)).Returns(1.5);

        var result = await CreateHandler().Handle(new ValidateDeliveryQuery(branch.Id, 12.91, 77.61), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsDeliverable.Should().BeTrue();
        result.Data.DistanceKm.Should().Be(1.5);
    }

    [Fact]
    public async Task Handle_AddressOutsideRadius_ReturnsNotDeliverable()
    {
        var branch = Branch.Create(Guid.NewGuid(), "City", 12.9, 77.6, "Addr", 5.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(12.9, 77.6, It.IsAny<double>(), It.IsAny<double>())).Returns(20.0);

        var result = await CreateHandler().Handle(new ValidateDeliveryQuery(branch.Id, 99.0, 88.0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsDeliverable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AddressExactlyAtBoundary_ReturnsDeliverable()
    {
        var branch = Branch.Create(Guid.NewGuid(), "City", 12.9, 77.6, "Addr", 5.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _geoService.Setup(g => g.CalculateDistanceKm(It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>())).Returns(5.0); // exactly at boundary

        var result = await CreateHandler().Handle(new ValidateDeliveryQuery(branch.Id, 0, 0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsDeliverable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BranchNotFound_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await CreateHandler().Handle(new ValidateDeliveryQuery(Guid.NewGuid(), 0, 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
