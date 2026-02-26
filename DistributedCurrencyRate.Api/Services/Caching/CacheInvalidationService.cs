using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace DistributedCurrencyRate.Api;

public sealed class CacheInvalidationService(IConnectionMultiplexer redis, HybridCache cache, ILogger<CacheInvalidationService> logger) : BackgroundService
{
    private readonly RedisChannel Channel = RedisChannel.Literal("cache-invalidation");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = redis.GetSubscriber();

        await sub.SubscribeAsync(Channel, async (_, value) =>
        {
            var key = value.ToString();
            logger.LogInformation("Invalidating local cache for: {Key}", key);
            await cache.RemoveAsync(key, stoppingToken);
        });
    }
}