using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace FoodFleet.Infrastructure.Services;

public class AdminCredentialStore(IConfiguration config) : IAdminCredentialStore
{
    public string Username => config["Admin:Username"] ?? "admin";
    public string PasswordHash => config["Admin:PasswordHash"] ?? string.Empty;
}
