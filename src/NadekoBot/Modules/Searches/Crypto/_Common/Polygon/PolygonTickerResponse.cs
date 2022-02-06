#nullable disable
using System.Text.Json.Serialization;

namespace NadekoBot.Modules.Searches;

public class PolygonTickerResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("results")]
    public List<PolygonTickerData> Results { get; set; }
}