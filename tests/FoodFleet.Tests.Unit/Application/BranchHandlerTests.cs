using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Branches;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class GetAllBranchesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public GetAllBranchesQueryHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedBranches()
    {
        var branches = new List<Branch>
        {
            Branch.Create(Guid.NewGuid(), "A", 1.0, 2.0, "Addr A", 5.0),
            Branch.Create(Guid.NewGuid(), "B", 3.0, 4.0, "Addr B", 7.0),
        };
        _branchRepo.Setup(r => r.GetActiveBranchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        var result = await new GetAllBranchesQueryHandler(_uow.Object)
            .Handle(new GetAllBranchesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoBranches_ReturnsEmptyList()
    {
        _branchRepo.Setup(r => r.GetActiveBranchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Branch>());

        var result = await new GetAllBranchesQueryHandler(_uow.Object)
            .Handle(new GetAllBranchesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }
}

public class GetBranchByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public GetBranchByIdQueryHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingBranch_ReturnsBranch()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Main", 1.0, 2.0, "Addr", 5.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);

        var result = await new GetBranchByIdQueryHandler(_uow.Object)
            .Handle(new GetBranchByIdQuery(branch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Main");
    }

    [Fact]
    public async Task Handle_NonExistentBranch_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await new GetBranchByIdQueryHandler(_uow.Object)
            .Handle(new GetBranchByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class GetNearestBranchQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public GetNearestBranchQueryHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
    }

    [Fact]
    public async Task Handle_BranchFound_ReturnsNearest()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Nearest", 12.9, 77.6, "Addr", 5.0);
        _branchRepo.Setup(r => r.FindNearestAsync(12.9, 77.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);

        var result = await new GetNearestBranchQueryHandler(_uow.Object)
            .Handle(new GetNearestBranchQuery(12.9, 77.6), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Nearest");
    }

    [Fact]
    public async Task Handle_NoBranchFound_Returns404()
    {
        _branchRepo.Setup(r => r.FindNearestAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await new GetNearestBranchQueryHandler(_uow.Object)
            .Handle(new GetNearestBranchQuery(0, 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class CreateBranchCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();
    private readonly Mock<IRestaurantRepository> _restaurantRepo = new();

    public CreateBranchCommandHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.Restaurants).Returns(_restaurantRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _branchRepo.Setup(r => r.AddAsync(It.IsAny<Branch>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ActiveRestaurantExists_Returns201()
    {
        var restaurant = Restaurant.Create("FoodFleet", "info@ff.com");
        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var request = new CreateBranchRequest("New Branch", 12.9, 77.6, "1 Main St", 5.0);
        var result = await new CreateBranchCommandHandler(_uow.Object)
            .Handle(new CreateBranchCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("New Branch");
    }

    [Fact]
    public async Task Handle_NoActiveRestaurant_Returns400()
    {
        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Restaurant?)null);

        var request = new CreateBranchRequest("Branch", 0, 0, "Addr", 5.0);
        var result = await new CreateBranchCommandHandler(_uow.Object)
            .Handle(new CreateBranchCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }
}

public class UpdateBranchCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public UpdateBranchCommandHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _branchRepo.Setup(r => r.Update(It.IsAny<Branch>()));
    }

    [Fact]
    public async Task Handle_ExistingBranch_UpdatesAndReturns()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Old", 1.0, 2.0, "Old Addr", 3.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var request = new UpdateBranchRequest("New Name", 5.0, 6.0, "New Addr", 10.0);
        var result = await new UpdateBranchCommandHandler(_uow.Object)
            .Handle(new UpdateBranchCommand(branch.Id, request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_BranchNotFound_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await new UpdateBranchCommandHandler(_uow.Object)
            .Handle(new UpdateBranchCommand(Guid.NewGuid(), new UpdateBranchRequest("N", 0, 0, "A", 3)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class DeleteBranchCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public DeleteBranchCommandHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _branchRepo.Setup(r => r.Update(It.IsAny<Branch>()));
    }

    [Fact]
    public async Task Handle_ExistingBranch_DeactivatesAndSucceeds()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Branch", 0, 0, "Addr");
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await new DeleteBranchCommandHandler(_uow.Object)
            .Handle(new DeleteBranchCommand(branch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        branch.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_BranchNotFound_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await new DeleteBranchCommandHandler(_uow.Object)
            .Handle(new DeleteBranchCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class UpdateDeliveryRadiusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBranchRepository> _branchRepo = new();

    public UpdateDeliveryRadiusCommandHandlerTests()
    {
        _uow.Setup(u => u.Branches).Returns(_branchRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _branchRepo.Setup(r => r.Update(It.IsAny<Branch>()));
    }

    [Fact]
    public async Task Handle_ValidRadius_UpdatesRadius()
    {
        var branch = Branch.Create(Guid.NewGuid(), "Branch", 0, 0, "Addr", 3.0);
        _branchRepo.Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(branch);

        var result = await new UpdateDeliveryRadiusCommandHandler(_uow.Object)
            .Handle(new UpdateDeliveryRadiusCommand(branch.Id, 15.0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.DeliveryRadiusKm.Should().Be(15.0);
    }

    [Fact]
    public async Task Handle_BranchNotFound_Returns404()
    {
        _branchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var result = await new UpdateDeliveryRadiusCommandHandler(_uow.Object)
            .Handle(new UpdateDeliveryRadiusCommand(Guid.NewGuid(), 10.0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
