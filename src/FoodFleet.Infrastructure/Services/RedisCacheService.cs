using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FoodFleet.Infrastructure.Services;

public class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var data = await cache.GetStringAsync(key, ct);
        return data is null ? null : JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry;
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value, _jsonOptions), options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);

    public Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // For Redis, we'd use SCAN with pattern; simplified here
        return Task.CompletedTask;
    }
}
