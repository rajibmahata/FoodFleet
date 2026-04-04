using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Auth.Commands;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public RegisterCommandHandlerTests()
    {
        _uow.Setup(u => u.Customers).Returns(_customerRepo.Object);
    }

    private RegisterCommandHandler CreateHandler() =>
        new(_uow.Object, _hasher.Object, _tokenService.Object);

    [Fact]
    public async Task Handle_NewEmail_Returns201WithTokens()
    {
        // Arrange
        _customerRepo.Setup(r => r.EmailExistsAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Passw0rd!")).Returns("hashed");
        _customerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var expiry = DateTime.UtcNow.AddMinutes(15);
        _tokenService.Setup(t => t.GenerateCustomerTokens(It.IsAny<Guid>(), "new@test.com", "Alice"))
            .Returns(new TokenResult("access_token", "refresh_token", expiry));

        var command = new RegisterCommand(new RegisterRequest("Alice", "new@test.com", "9876543210", "Passw0rd!"));

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.AccessToken.Should().Be("access_token");
        result.Data.Customer.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_Returns409()
    {
        _customerRepo.Setup(r => r.EmailExistsAsync("dup@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterCommand(new RegisterRequest("Dup", "dup@test.com", "123", "pass"));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task Handle_SavesCustomerToRepository()
    {
        _customerRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _customerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tokenService.Setup(t => t.GenerateCustomerTokens(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TokenResult("at", "rt", DateTime.UtcNow.AddMinutes(15)));

        var command = new RegisterCommand(new RegisterRequest("Alice", "alice@test.com", "123", "Passw0rd!"));
        await CreateHandler().Handle(command, CancellationToken.None);

        _customerRepo.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public LoginCommandHandlerTests()
    {
        _uow.Setup(u => u.Customers).Returns(_customerRepo.Object);
    }

    private LoginCommandHandler CreateHandler() =>
        new(_uow.Object, _hasher.Object, _tokenService.Object);

    [Fact]
    public async Task Handle_CorrectCredentials_Returns200WithTokens()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "123", "hashed");
        _customerRepo.Setup(r => r.GetByEmailAsync("alice@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("Passw0rd!", "hashed")).Returns(true);
        _tokenService.Setup(t => t.GenerateCustomerTokens(It.IsAny<Guid>(), "alice@test.com", "Alice"))
            .Returns(new TokenResult("access", "refresh", DateTime.UtcNow.AddMinutes(15)));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new LoginCommand(new LoginRequest("alice@test.com", "Passw0rd!")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data!.AccessToken.Should().Be("access");
    }

    [Fact]
    public async Task Handle_WrongPassword_Returns401()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "123", "hashed");
        _customerRepo.Setup(r => r.GetByEmailAsync("alice@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("wrongpass", "hashed")).Returns(false);

        var result = await CreateHandler().Handle(
            new LoginCommand(new LoginRequest("alice@test.com", "wrongpass")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_UnknownEmail_Returns401()
    {
        _customerRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await CreateHandler().Handle(
            new LoginCommand(new LoginRequest("ghost@test.com", "any")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }
}
