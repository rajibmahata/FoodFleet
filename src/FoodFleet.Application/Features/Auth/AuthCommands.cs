using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using MediatR;

namespace FoodFleet.Application.Features.Auth.Commands;

// ── Register ──────────────────────────────────────────────────
public record RegisterCommand(RegisterRequest Request) : IRequest<Result<AuthResponse>>;

public class RegisterCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokenService) 
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await uow.Customers.EmailExistsAsync(cmd.Request.Email, ct))
            return Result<AuthResponse>.Failure("Email already registered.", 409);

        var hash = hasher.Hash(cmd.Request.Password);
        var customer = Customer.Create(cmd.Request.Name, cmd.Request.Email, cmd.Request.Phone, hash);
        await uow.Customers.AddAsync(customer, ct);

        var tokens = tokenService.GenerateCustomerTokens(customer.Id, customer.Email, customer.Name);
        customer.SetRefreshToken(tokens.RefreshToken, tokens.AccessTokenExpiry.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var dto = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt);
        return Result<AuthResponse>.Success(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, dto), 201);
    }
}

// ── Login ─────────────────────────────────────────────────────
public record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;

public class LoginCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokenService)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByEmailAsync(cmd.Request.Email, ct);
        if (customer == null || !hasher.Verify(cmd.Request.Password, customer.PasswordHash))
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);

        var tokens = tokenService.GenerateCustomerTokens(customer.Id, customer.Email, customer.Name);
        customer.SetRefreshToken(tokens.RefreshToken, tokens.AccessTokenExpiry.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var dto = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt);
        return Result<AuthResponse>.Success(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, dto));
    }
}

// ── Admin Login ───────────────────────────────────────────────
public record AdminLoginCommand(AdminLoginRequest Request) : IRequest<Result<AdminAuthResponse>>;

public class AdminLoginCommandHandler(IPasswordHasher hasher, ITokenService tokenService, IAdminCredentialStore credentials)
    : IRequestHandler<AdminLoginCommand, Result<AdminAuthResponse>>
{
    public Task<Result<AdminAuthResponse>> Handle(AdminLoginCommand cmd, CancellationToken ct)
    {
        if (!string.Equals(cmd.Request.Username, credentials.Username, StringComparison.OrdinalIgnoreCase)
            || !hasher.Verify(cmd.Request.Password, credentials.PasswordHash))
            return Task.FromResult(Result<AdminAuthResponse>.Unauthorized("Invalid credentials."));

        var roles = new[] { "Admin" };
        var tokens = tokenService.GenerateAdminTokens(credentials.Username, roles);
        return Task.FromResult(Result<AdminAuthResponse>.Success(
            new AdminAuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, credentials.Username, roles)));
    }
}

public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler(IUnitOfWork uow, ITokenService tokenService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var customerId = tokenService.ValidateToken(cmd.Request.RefreshToken);
        if (customerId == null)
            return Result<AuthResponse>.Unauthorized("Invalid refresh token.");

        var customer = await uow.Customers.GetByIdAsync(customerId.Value, ct);
        if (customer == null || !customer.IsRefreshTokenValid(cmd.Request.RefreshToken))
            return Result<AuthResponse>.Unauthorized("Refresh token expired or invalid.");

        var tokens = tokenService.GenerateCustomerTokens(customer.Id, customer.Email, customer.Name);
        customer.SetRefreshToken(tokens.RefreshToken, tokens.AccessTokenExpiry.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var dto = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt);
        return Result<AuthResponse>.Success(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, dto));
    }
}
