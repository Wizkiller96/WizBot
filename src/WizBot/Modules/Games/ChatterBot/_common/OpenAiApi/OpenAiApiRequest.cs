using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class OpenAiApiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; }

    [JsonPropertyName("messages")]
    public List<OpenAiApiMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public int Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; }
}