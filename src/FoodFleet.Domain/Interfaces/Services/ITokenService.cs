namespace FoodFleet.Domain.Interfaces.Services;

public record TokenResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);

public interface ITokenService
{
    TokenResult GenerateCustomerTokens(Guid customerId, string email, string name);
    TokenResult GenerateAdminTokens(string username, IEnumerable<string> roles);
    Guid? ValidateToken(string token);
}
