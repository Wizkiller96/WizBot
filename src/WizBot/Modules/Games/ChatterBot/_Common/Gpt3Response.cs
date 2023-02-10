using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class Gpt3Response
{
    [JsonPropertyName("choices")]
    public Choice[] Choices { get; set; }
}

public class Choice
{
    public string Text { get; set; }
}

public class Gpt3ApiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; }

    [JsonPropertyName("temperature")]
    public int Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; }
}