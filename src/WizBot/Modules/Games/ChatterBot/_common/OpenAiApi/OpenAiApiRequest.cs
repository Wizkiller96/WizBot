using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class OpenAiApiRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<OpenAiApiMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public required int Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; init; }
}