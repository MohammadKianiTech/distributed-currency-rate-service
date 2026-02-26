using System.Collections.Concurrent;

namespace DistributedCurrencyRate.Api;

public static class CurrencyConversion1
{
    public static async Task<IResult> Handle(
        string currencyCode,
        decimal amount,
        CurrencyApiClient currencyClient)
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

        var rate = await GetExchangeRate(currencyCode, currencyClient);

        if (rate == null)
        {
            return Results.NotFound(new { error = $"Exchange rate for {currencyCode} not found or API error occured." });
        }

        var convertedAmount = amount * rate.Value;

        return Results.Ok(new ExchangeRateResponse(
            Currency: currencyCode,
            BaseCurrency: "USD",
            Rate: rate.Value,
            Amount: amount,
            ConvertedAmount: convertedAmount));
    }
    private record CacheEntry(decimal Rate, DateTime CreatedAtUtc);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private static async Task<decimal?> GetExchangeRate(string currencyCode, CurrencyApiClient currencyClient)
    {
        if (Cache.TryGetValue(currencyCode, out var entry) && IsFresh(entry))
        {
            return entry.Rate;
        }

        var semaphore = Locks.GetOrAdd(currencyCode, _ => new SemaphoreSlim(1, 1));

        var acquired = await semaphore.WaitAsync(TimeSpan.FromMinutes(5));
        if (!acquired)
        {
            throw new TimeoutException("Could not acquired exchange rates. Try again later");
        }

        try
        {
            if (Cache.TryGetValue(currencyCode, out entry) && IsFresh(entry))
            {
                return entry.Rate;
            }

            var currentRate = await currencyClient.GetEchangeRateAsync(currencyCode);

            if (currentRate != null)
            {
                Cache.AddOrUpdate(
                    currencyCode,
                    _ => new CacheEntry(currentRate.Value, DateTime.UtcNow),
                    (_, _) => new CacheEntry(currentRate.Value, DateTime.UtcNow));
            }
            return currentRate;
        }
        finally
        {
            semaphore.Release();
            if (semaphore.CurrentCount == 1)
                Locks.TryRemove(currencyCode, out _);
        }
    }

    private static bool IsFresh(CacheEntry entry)
    {
        return DateTime.UtcNow - entry.CreatedAtUtc < CacheDuration;
    }
}