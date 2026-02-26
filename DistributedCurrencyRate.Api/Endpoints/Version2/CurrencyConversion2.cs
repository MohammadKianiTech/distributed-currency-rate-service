using Microsoft.Extensions.Caching.Hybrid;

namespace DistributedCurrencyRate.Api;

public static class CurrencyConversion2
{
    public static async Task<IResult> Handle(
        string currencyCode,
        decimal amount,
        CurrencyApiClient currencyClient,
        HybridCache cache,
        CancellationToken cancellationToken = default)
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

        var cacheKey = $"ExchangeRate_{currencyCode}";
        var cachedRate = await cache.GetOrCreateAsync(cacheKey, async token =>
        {
            var rate = await GetExchangeRate(currencyCode, currencyClient, token);
            return rate;
        },
        tags: ["currency"],
        cancellationToken: cancellationToken);

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
    private static async Task<decimal?> GetExchangeRate(string currencyCode, CurrencyApiClient currencyClient, CancellationToken cancellationToken)
    {
        var currentRate = await currencyClient.GetEchangeRateAsync(currencyCode);
        return currentRate;
    }

}