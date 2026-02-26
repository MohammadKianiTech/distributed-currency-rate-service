using System.Text.Json;

namespace DistributedCurrencyRate.Api;

public class CurrencyApiClient(IHttpClientFactory httpClientFactory, ILogger<CurrencyApiClient> logger)
{
    public async Task<decimal?> GetEchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogWarning("CALLING EXTERNAL API FOR {Currency}", currencyCode);
            using HttpClient client = CreateCurrencyClient();

            HttpResponseMessage response = await client.GetAsync("latest", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("API request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<CurrencyApiResponse>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, cancellationToken
            );

            if (apiResponse?.Data != null &&
                apiResponse.Data.TryGetValue(currencyCode.ToUpperInvariant(), out var exchangeRate))
            {
                return exchangeRate;
            }
            return null;
        }
        catch (Exception)
        {
            logger.LogError("Error fetching exchange rate for {Currecny}", currencyCode);
            return null;
        }
    }
    private HttpClient CreateCurrencyClient()
    {
        HttpClient client = httpClientFactory.CreateClient("currency");
        return client;
    }
}
