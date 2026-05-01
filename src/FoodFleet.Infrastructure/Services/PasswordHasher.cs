using FoodFleet.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace FoodFleet.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 310_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, HashSize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var storedHashBytes = Convert.FromBase64String(parts[1]);
            var computedHash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, HashSize);
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
        }
        catch
        {
            return false;
        }
    }
}
