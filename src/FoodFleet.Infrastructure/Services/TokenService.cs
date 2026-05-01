using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FoodFleet.Infrastructure.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    private readonly string _secret = config["Jwt:Key"] ?? throw new InvalidOperationException("JWT secret not configured.");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "FoodFleet";
    private readonly string _audience = config["Jwt:Audience"] ?? "FoodFleet";
    private readonly int _accessTokenMinutes = int.TryParse(config["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 15;

    public TokenResult GenerateCustomerTokens(Guid customerId, string email, string name)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customerId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim("role", "Customer")
        };
        return GenerateTokens(claims);
    }

    public TokenResult GenerateAdminTokens(string username, IEnumerable<string> roles)
    {
        var claims = roles
            .Select(r => new Claim("role", r))
            .Prepend(new Claim(ClaimTypes.Name, username))
            .ToArray();
        return GenerateTokens(claims);
    }

    public Guid? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var result = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false  // refresh token can be expired
            }, out _);

            var idClaim = result.FindFirst(ClaimTypes.NameIdentifier);
            return idClaim is not null && Guid.TryParse(idClaim.Value, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    private TokenResult GenerateTokens(Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var expiry = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new TokenResult(accessToken, refreshToken, expiry);
    }
}
