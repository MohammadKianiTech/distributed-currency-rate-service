using Medallion.Threading;
using Microsoft.Extensions.Caching.Hybrid;

namespace DistributedCurrencyRate.Api;

public static class CurrencyConversion3
{
    public static async Task<IResult> Handle(
        string currencyCode,
        decimal amount,
        HybridCache cache,
        IDistributedLockProvider lockProvider,
        CurrencyApiClient client,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 || !currencyCode.All(char.IsLetter))
        {
            return Results.BadRequest(new { error = "Currecny code must be a 3-letter uppercase code (e.g., EUR, GBP)" });
        }
        //validate amount(must be positive)
        if (amount < 0)
        {
            return Results.BadRequest(new { error = "Amount must be a positive number" });
        }

        var key = $"ExchangeRate_{currencyCode}";

        var cachedRate = await cache.GetOrCreateAsync(
            key,
            async cancel =>
            {
                await using var handle = await lockProvider.AcquireLockAsync($"lock:{key}", cancellationToken: cancel);

                return await client.GetEchangeRateAsync(currencyCode, cancel);
            },
            tags: ["currency"],
            cancellationToken: ct);

        if (cachedRate == null)
        {
            return Results.NotFound(new { error = $"Exchange rate for {currencyCode} not found or API error occured." });
        }
        var convertedAmount = amount * cachedRate.Value;

        return Results.Ok(new ExchangeRateResponse(
            Currency: currencyCode,
            BaseCurrency: "USD",
            Rate: cachedRate.Value,
            Amount: amount,
            ConvertedAmount: convertedAmount));
    }
}