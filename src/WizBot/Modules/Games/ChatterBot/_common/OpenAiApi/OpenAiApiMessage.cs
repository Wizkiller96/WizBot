using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class OpenAiApiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; }
}