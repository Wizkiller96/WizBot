using System.Text.Json.Serialization;

namespace WizBot.Modules.Utility;

public sealed class CommandPromptResultModel
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("arguments")]
    public required Dictionary<string, string> Arguments { get; set; }
    
    [JsonPropertyName("remaining")]
    [JsonConverter(typeof(NumberToStringConverter))]
    public required string Remaining { get; set; }
}