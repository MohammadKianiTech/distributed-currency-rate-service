using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace DistributedCurrencyRate.Api;

public interface ICacheInvalidator
{
    Task InvalidateAsync(string currencyCode, CancellationToken cancellationToken = default);
}
public sealed class RedisCacheInvalidator(
    IConnectionMultiplexer connectionMultiplexer,
    HybridCache cache,
    ILogger<RedisCacheInvalidator> logger) : ICacheInvalidator
{
    private readonly RedisChannel Channel = RedisChannel.Literal("cache-invalidation");
    public async Task InvalidateAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var key = $"ExchangeRate_{currencyCode}";
        await cache.RemoveAsync(key, cancellationToken);
        var subscriber = connectionMultiplexer.GetSubscriber();
        await subscriber.PublishAsync(Channel, key);
        logger.LogInformation("Published invalidation for key: {Key}", key);
    }
}