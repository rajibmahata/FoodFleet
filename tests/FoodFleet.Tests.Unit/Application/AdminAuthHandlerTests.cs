using FoodFleet.Application.Features.Auth.Commands;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace FoodFleet.Tests.Unit.Application;

public class AdminLoginCommandHandlerTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAdminCredentialStore> _credentials = new();

    private AdminLoginCommandHandler CreateHandler() =>
        new(_hasher.Object, _tokenService.Object, _credentials.Object);

    [Fact]
    public async Task Handle_CorrectCredentials_ReturnsTokens()
    {
        _credentials.Setup(c => c.Username).Returns("admin");
        _credentials.Setup(c => c.PasswordHash).Returns("hash");
        _hasher.Setup(h => h.Verify("secret", "hash")).Returns(true);

        var expiry = DateTime.UtcNow.AddMinutes(15);
        _tokenService.Setup(t => t.GenerateAdminTokens("admin", It.IsAny<string[]>()))
            .Returns(new TokenResult("at", "rt", expiry));

        var result = await CreateHandler().Handle(
            new AdminLoginCommand(new AdminLoginRequest("admin", "secret")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("at");
        result.Data.Username.Should().Be("admin");
    }

    [Fact]
    public async Task Handle_WrongPassword_Returns401()
    {
        _credentials.Setup(c => c.Username).Returns("admin");
        _credentials.Setup(c => c.PasswordHash).Returns("hash");
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await CreateHandler().Handle(
            new AdminLoginCommand(new AdminLoginRequest("admin", "wrong")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WrongUsername_Returns401()
    {
        _credentials.Setup(c => c.Username).Returns("admin");
        _credentials.Setup(c => c.PasswordHash).Returns("hash");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "hash")).Returns(false);

        var result = await CreateHandler().Handle(
            new AdminLoginCommand(new AdminLoginRequest("notadmin", "secret")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }
}

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public RefreshTokenCommandHandlerTests()
    {
        _uow.Setup(u => u.Customers).Returns(_customerRepo.Object);
    }

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_uow.Object, _tokenService.Object);

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokens()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "123", "hash");
        var oldRefreshToken = "valid_refresh_token";
        var refreshExpiry = DateTime.UtcNow.AddDays(7);
        // Simulate that the customer has this refresh token set
        customer.SetRefreshToken(oldRefreshToken, refreshExpiry);

        _tokenService.Setup(t => t.ValidateToken(oldRefreshToken)).Returns(customer.Id);
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var expiry = DateTime.UtcNow.AddMinutes(15);
        _tokenService.Setup(t => t.GenerateCustomerTokens(customer.Id, customer.Email, customer.Name))
            .Returns(new TokenResult("new_access", "new_refresh", expiry));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new RefreshTokenCommand(new RefreshTokenRequest(oldRefreshToken)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new_access");
    }

    [Fact]
    public async Task Handle_InvalidToken_Returns401()
    {
        _tokenService.Setup(t => t.ValidateToken("bad_token")).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("bad_token")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_Returns401()
    {
        var customerId = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateToken("valid_token")).Returns(customerId);
        _customerRepo.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await CreateHandler().Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("valid_token")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_ExpiredStoredRefreshToken_Returns401()
    {
        var customer = Customer.Create("Alice", "alice@test.com", "123", "hash");
        // Set an expired refresh token
        customer.SetRefreshToken("expired_token", DateTime.UtcNow.AddDays(-1));

        _tokenService.Setup(t => t.ValidateToken("expired_token")).Returns(customer.Id);
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await CreateHandler().Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("expired_token")),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }
}
