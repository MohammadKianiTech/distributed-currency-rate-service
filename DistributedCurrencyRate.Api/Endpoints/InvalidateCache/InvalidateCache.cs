namespace DistributedCurrencyRate.Api;

public static class InvalidateCache
{
    public static async Task<IResult> Handle(
        string currencyCode,
        ICacheInvalidator cacheInvalidator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 || !currencyCode.All(char.IsLetter))
        {
            return Results.BadRequest(new { error = "Currecny code must be a 3-letter uppercase code (e.g., EUR, GBP)" });
        }

        await cacheInvalidator.InvalidateAsync(currencyCode, cancellationToken);

        return Results.NoContent();
    }
}