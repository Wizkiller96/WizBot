using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class OpenAiApiMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}