namespace DistributedCurrencyRate.Api;

public static class EndpointExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api/v1");

        apiGroup.MapGet("/convert/{currencyCode}", CurrencyConversion1.Handle)
            .WithName("CurrencyConversion1")
            .WithSummary("SemaphoreSlim (single-node locking)")
            .RequireRateLimiting("fixed");

        apiGroup.MapGet("/convert2/{currencyCode}", CurrencyConversion2.Handle)
            .WithName("CurrencyConversion2")
            .WithSummary("HybridCache (L1 + Redis L2 distributed caching)")
            .RequireRateLimiting("fixed");

        apiGroup.MapGet("/convert3/{currencyCode}", CurrencyConversion3.Handle)
            .WithName("CurrencyConversion3")
            .WithSummary("HybridCache + Distributed Lock (multi-node coordination)");
        // .RequireRateLimiting("fixed");

        apiGroup.MapDelete("/{currencyCode}/invalidate-cache", InvalidateCache.Handle)
            .WithName("invalidate-cache")
            .RequireRateLimiting("fixed");
        return app;
    }
}