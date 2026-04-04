using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FoodFleet.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? CustomerId
    {
        get
        {
            var claim = User?.FindFirstValue("customerId");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAdmin
    {
        get
        {
            var role = User?.FindFirstValue(ClaimTypes.Role);
            return role == "Admin" || role == "SuperAdmin";
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
