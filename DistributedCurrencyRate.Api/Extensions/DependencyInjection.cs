using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DistributedCurrencyRate.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        return builder;
    }
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient().ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler());
        builder.Services.AddTransient<CurrencyApiClient>();
        builder.Services.Configure<CurrencySettings>(builder.Configuration.GetSection("CurrencyApi"));
        builder.Services.AddHttpClient("currency", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<CurrencySettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("apikey", settings.ApiKey);
        });
        return builder;
    }
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var cs = builder.Configuration.GetConnectionString("Redis")!;
            options.Configuration = cs;
        });
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Expiration = TimeSpan.FromMinutes(5)
            };
        });
        // Requires StackExchange.Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cs = builder.Configuration.GetConnectionString("Redis")!;
            return ConnectionMultiplexer.Connect(cs);
        });
        // Register the distributed lock provider
        builder.Services.AddSingleton<IDistributedLockProvider>((sp) =>
        {
            var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return new RedisDistributedSynchronizationProvider(connectionMultiplexer.GetDatabase());
        });
        // Register our invalidation services
        builder.Services.AddSingleton<ICacheInvalidator, RedisCacheInvalidator>();
        builder.Services.AddHostedService<CacheInvalidationService>();
        return builder;
    }
    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });
        return builder;
    }
}