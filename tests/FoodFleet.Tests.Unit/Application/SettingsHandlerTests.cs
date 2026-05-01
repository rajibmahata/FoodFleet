using FoodFleet.Application.Features.Settings;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class GetActiveRestaurantSettingsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRestaurantRepository> _restaurantRepo = new();
    private readonly Mock<ISettingsRepository> _settingsRepo = new();

    public GetActiveRestaurantSettingsQueryHandlerTests()
    {
        _uow.Setup(u => u.Restaurants).Returns(_restaurantRepo.Object);
        _uow.Setup(u => u.Settings).Returns(_settingsRepo.Object);
    }

    [Fact]
    public async Task Handle_ActiveRestaurantExists_ReturnsSettings()
    {
        var restaurant = Restaurant.Create("FoodFleet HQ", "hq@foodfleet.com");
        var settings = new List<RestaurantSettings>
        {
            RestaurantSettings.Create(restaurant.Id, "currency", "INR"),
            RestaurantSettings.Create(restaurant.Id, "timezone", "Asia/Kolkata"),
        };

        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);
        _settingsRepo.Setup(r => r.GetAllByRestaurantAsync(restaurant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await new GetActiveRestaurantSettingsQueryHandler(_uow.Object)
            .Handle(new GetActiveRestaurantSettingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data.Should().Contain(s => s.Key == "currency" && s.Value == "INR");
    }

    [Fact]
    public async Task Handle_NoActiveRestaurant_Returns404()
    {
        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Restaurant?)null);

        var result = await new GetActiveRestaurantSettingsQueryHandler(_uow.Object)
            .Handle(new GetActiveRestaurantSettingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}

public class GetSettingsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISettingsRepository> _settingsRepo = new();

    public GetSettingsQueryHandlerTests()
    {
        _uow.Setup(u => u.Settings).Returns(_settingsRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSettingsForRestaurant()
    {
        var restaurantId = Guid.NewGuid();
        var settings = new List<RestaurantSettings>
        {
            RestaurantSettings.Create(restaurantId, "min_order", "100"),
        };
        _settingsRepo.Setup(r => r.GetAllByRestaurantAsync(restaurantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await new GetSettingsQueryHandler(_uow.Object)
            .Handle(new GetSettingsQuery(restaurantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(1);
    }
}

public class UpsertActiveRestaurantSettingsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRestaurantRepository> _restaurantRepo = new();
    private readonly Mock<ISettingsRepository> _settingsRepo = new();

    public UpsertActiveRestaurantSettingsCommandHandlerTests()
    {
        _uow.Setup(u => u.Restaurants).Returns(_restaurantRepo.Object);
        _uow.Setup(u => u.Settings).Returns(_settingsRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _settingsRepo.Setup(r => r.UpsertAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ActiveRestaurant_UpsertsAllSettings()
    {
        var restaurant = Restaurant.Create("FoodFleet HQ", "hq@foodfleet.com");
        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(restaurant);

        var settings = new List<UpsertSettingRequest>
        {
            new("currency", "USD"),
            new("tax_rate", "0.08"),
        };

        var result = await new UpsertActiveRestaurantSettingsCommandHandler(_uow.Object)
            .Handle(new UpsertActiveRestaurantSettingsCommand(settings), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _settingsRepo.Verify(r => r.UpsertAsync(restaurant.Id, "currency", "USD",
            It.IsAny<CancellationToken>()), Times.Once);
        _settingsRepo.Verify(r => r.UpsertAsync(restaurant.Id, "tax_rate", "0.08",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoActiveRestaurant_Returns404()
    {
        _restaurantRepo.Setup(r => r.GetFirstActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Restaurant?)null);

        var result = await new UpsertActiveRestaurantSettingsCommandHandler(_uow.Object)
            .Handle(new UpsertActiveRestaurantSettingsCommand(new List<UpsertSettingRequest>()),
                CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
