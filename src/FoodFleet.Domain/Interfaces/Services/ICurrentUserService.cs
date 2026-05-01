namespace FoodFleet.Domain.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? CustomerId { get; }
    string? Email { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
