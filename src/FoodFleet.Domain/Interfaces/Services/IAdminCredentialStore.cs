namespace FoodFleet.Domain.Interfaces.Services;

/// <summary>Provides admin credentials (username + hashed password) for authentication.
/// Credentials are configured via appsettings / environment variables and never stored in the DB.</summary>
public interface IAdminCredentialStore
{
    string Username { get; }
    string PasswordHash { get; }
}
