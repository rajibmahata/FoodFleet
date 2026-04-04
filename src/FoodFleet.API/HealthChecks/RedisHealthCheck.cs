using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodFleet.API.HealthChecks;

/// <summary>Pings the configured distributed cache (Redis) with a trivial get.</summary>
public class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    private static readonly byte[] Ping = "1"u8.ToArray();
    private const string Key = "health:ping";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetAsync(Key, Ping,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) },
                cancellationToken);

            var val = await cache.GetAsync(Key, cancellationToken);
            return val is { Length: > 0 }
                ? HealthCheckResult.Healthy("Cache is responsive.")
                : HealthCheckResult.Degraded("Cache returned empty value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache is unavailable.", ex);
        }
    }
}
