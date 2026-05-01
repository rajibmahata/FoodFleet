using FoodFleet.Application.Features.Menu;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class GetMenuCategoriesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuCategoryRepository> _categoryRepo = new();

    public GetMenuCategoriesQueryHandlerTests()
    {
        _uow.Setup(u => u.MenuCategories).Returns(_categoryRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllCategoriesForBranch()
    {
        var branchId = Guid.NewGuid();
        var cats = new List<MenuCategory>
        {
            MenuCategory.Create(branchId, "Starters", 1),
            MenuCategory.Create(branchId, "Mains", 2),
        };
        _categoryRepo.Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cats);

        var result = await new GetMenuCategoriesQueryHandler(_uow.Object)
            .Handle(new GetMenuCategoriesQuery(branchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }
}

public class GetMenuItemsByCategoryQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public GetMenuItemsByCategoryQueryHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsItemsForCategory()
    {
        var catId = Guid.NewGuid();
        var items = new List<MenuItem>
        {
            MenuItem.Create(catId, "Burger", 9.99m, "Juicy", null, null),
            MenuItem.Create(catId, "Fries", 3.49m, "Crispy", null, 100),
        };
        _itemRepo.Setup(r => r.GetByCategoryAsync(catId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await new GetMenuItemsByCategoryQueryHandler(_uow.Object)
            .Handle(new GetMenuItemsByCategoryQuery(catId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
    }
}

public class CreateMenuCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuCategoryRepository> _categoryRepo = new();

    public CreateMenuCategoryCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuCategories).Returns(_categoryRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _categoryRepo.Setup(r => r.AddAsync(It.IsAny<MenuCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ValidRequest_Returns201WithCategory()
    {
        var branchId = Guid.NewGuid();
        var request = new CreateMenuCategoryRequest(branchId, "Desserts", 3);

        var result = await new CreateMenuCategoryCommandHandler(_uow.Object)
            .Handle(new CreateMenuCategoryCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("Desserts");
        _categoryRepo.Verify(r => r.AddAsync(It.IsAny<MenuCategory>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UpdateMenuCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuCategoryRepository> _categoryRepo = new();

    public UpdateMenuCategoryCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuCategories).Returns(_categoryRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _categoryRepo.Setup(r => r.Update(It.IsAny<MenuCategory>()));
    }

    [Fact]
    public async Task Handle_ExistingCategory_UpdatesAndReturns()
    {
        var category = MenuCategory.Create(Guid.NewGuid(), "Old Name", 1);
        _categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await new UpdateMenuCategoryCommandHandler(_uow.Object)
            .Handle(new UpdateMenuCategoryCommand(category.Id, new UpdateMenuCategoryRequest("New Name", 2)),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_Returns404()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuCategory?)null);

        var result = await new UpdateMenuCategoryCommandHandler(_uow.Object)
            .Handle(new UpdateMenuCategoryCommand(Guid.NewGuid(), new UpdateMenuCategoryRequest("N", 1)),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class DeleteMenuCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuCategoryRepository> _categoryRepo = new();

    public DeleteMenuCategoryCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuCategories).Returns(_categoryRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _categoryRepo.Setup(r => r.Update(It.IsAny<MenuCategory>()));
    }

    [Fact]
    public async Task Handle_ExistingCategory_Deactivates()
    {
        var category = MenuCategory.Create(Guid.NewGuid(), "Drinks", 4);
        _categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await new DeleteMenuCategoryCommandHandler(_uow.Object)
            .Handle(new DeleteMenuCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CategoryNotFound_Returns404()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuCategory?)null);

        var result = await new DeleteMenuCategoryCommandHandler(_uow.Object)
            .Handle(new DeleteMenuCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class CreateMenuItemCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public CreateMenuItemCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _itemRepo.Setup(r => r.AddAsync(It.IsAny<MenuItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ValidRequest_Returns201WithItem()
    {
        var catId = Guid.NewGuid();
        var request = new CreateMenuItemRequest(catId, "Pizza", 12.99m, "Cheesy", null, 50);

        var result = await new CreateMenuItemCommandHandler(_uow.Object)
            .Handle(new CreateMenuItemCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("Pizza");
        result.Data.Price.Should().Be(12.99m);
    }
}

public class UpdateMenuItemCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public UpdateMenuItemCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _itemRepo.Setup(r => r.Update(It.IsAny<MenuItem>()));
    }

    [Fact]
    public async Task Handle_ExistingItem_UpdatesAndReturns()
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Old Burger", 9.99m, "Desc", null, null);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await new UpdateMenuItemCommandHandler(_uow.Object)
            .Handle(new UpdateMenuItemCommand(item.Id,
                new UpdateMenuItemRequest("New Burger", "Better", 10.99m, null)),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("New Burger");
        result.Data.Price.Should().Be(10.99m);
    }

    [Fact]
    public async Task Handle_ItemNotFound_Returns404()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuItem?)null);

        var result = await new UpdateMenuItemCommandHandler(_uow.Object)
            .Handle(new UpdateMenuItemCommand(Guid.NewGuid(),
                new UpdateMenuItemRequest("X", "Y", 1m, null)),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class DeleteMenuItemCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public DeleteMenuItemCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _itemRepo.Setup(r => r.Remove(It.IsAny<MenuItem>()));
    }

    [Fact]
    public async Task Handle_ExistingItem_Removes()
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Salad", 6.99m, null, null, null);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await new DeleteMenuItemCommandHandler(_uow.Object)
            .Handle(new DeleteMenuItemCommand(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _itemRepo.Verify(r => r.Remove(item), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemNotFound_Returns404()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuItem?)null);

        var result = await new DeleteMenuItemCommandHandler(_uow.Object)
            .Handle(new DeleteMenuItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class UpdateStockCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public UpdateStockCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _itemRepo.Setup(r => r.Update(It.IsAny<MenuItem>()));
    }

    [Fact]
    public async Task Handle_ExistingItem_UpdatesStock()
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Soup", 5.00m, null, null, 10);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await new UpdateStockCommandHandler(_uow.Object)
            .Handle(new UpdateStockCommand(item.Id, 25), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.StockCount.Should().Be(25);
    }

    [Fact]
    public async Task Handle_ItemNotFound_Returns404()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuItem?)null);

        var result = await new UpdateStockCommandHandler(_uow.Object)
            .Handle(new UpdateStockCommand(Guid.NewGuid(), 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class ToggleAvailabilityCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMenuItemRepository> _itemRepo = new();

    public ToggleAvailabilityCommandHandlerTests()
    {
        _uow.Setup(u => u.MenuItems).Returns(_itemRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _itemRepo.Setup(r => r.Update(It.IsAny<MenuItem>()));
    }

    [Fact]
    public async Task Handle_SetAvailableTrue_MakesItemAvailable()
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Wrap", 7.50m, null, null, null);
        item.SetAvailability(false);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await new ToggleAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleAvailabilityCommand(item.Id, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetAvailableFalse_MakesItemUnavailable()
    {
        var item = MenuItem.Create(Guid.NewGuid(), "Wrap", 7.50m, null, null, null);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await new ToggleAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleAvailabilityCommand(item.Id, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ItemNotFound_Returns404()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuItem?)null);

        var result = await new ToggleAvailabilityCommandHandler(_uow.Object)
            .Handle(new ToggleAvailabilityCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
