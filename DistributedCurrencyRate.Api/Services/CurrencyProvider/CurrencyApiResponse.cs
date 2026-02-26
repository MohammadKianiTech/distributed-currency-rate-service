using System.Text.Json.Serialization;

namespace DistributedCurrencyRate.Api;

public class CurrencyApiResponse
{
    [JsonPropertyName("data")]
    public Dictionary<string, decimal> Data { get; set; } = [];
}
