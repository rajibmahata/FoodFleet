using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace FoodFleet.Admin.Services;

public class AdminAuthStateProvider(ILocalStorageService localStorage, AdminApiClient apiClient)
    : AuthenticationStateProvider
{
    private const string TokenKey = "admin_access_token";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token;
        try
        {
            token = await localStorage.GetItemAsStringAsync(TokenKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available during SSR prerendering — return anonymous state
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = ParseClaimsFromJwt(token);
        var expiryClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expiryClaim is not null && long.TryParse(expiryClaim.Value, out var exp))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            if (expiry < DateTime.UtcNow)
            {
                await localStorage.RemoveItemAsync(TokenKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        await apiClient.SetTokenAsync(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await apiClient.SetTokenAsync(token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await apiClient.ClearTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.').ElementAtOrDefault(1);
        if (payload is null) return claims;

        // Pad base64url properly
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        var json = System.Text.Encoding.UTF8.GetString(bytes);

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (dict is null) return claims;

        foreach (var kvp in dict)
        {
            var key = kvp.Key switch
            {
                "sub" => ClaimTypes.NameIdentifier,
                "email" => ClaimTypes.Email,
                "name" => ClaimTypes.Name,
                "role" => ClaimTypes.Role,
                _ => kvp.Key
            };

            if (kvp.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in kvp.Value.EnumerateArray())
                    claims.Add(new Claim(key, item.GetString() ?? string.Empty));
            }
            else
            {
                claims.Add(new Claim(key, kvp.Value.ToString()));
            }
        }

        return claims;
    }
}
