using System.Text.Json.Serialization;

namespace WizBot.Modules.Searches;

public class YahooQueryModel
{
    [JsonPropertyName("quoteResponse")]
    public QuoteResponse QuoteResponse { get; set; } = null!;
}