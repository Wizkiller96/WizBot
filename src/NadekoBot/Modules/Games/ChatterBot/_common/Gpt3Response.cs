#nullable disable
using System.Text.Json.Serialization;

namespace NadekoBot.Modules.Games.Common.ChatterBot;

public class Gpt3Response
{
    [JsonPropertyName("choices")]
    public Choice[] Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message Message { get; init; }
}

public class Message {
    [JsonPropertyName("content")]
    public string Content { get; init; }
}

public class Gpt3ApiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; }

    [JsonPropertyName("messages")]
    public List<GPTMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public int Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; }
}

public class GPTMessage
{
    [JsonPropertyName("role")]
    public string Role {get; init;}
    [JsonPropertyName("content")]
    public string Content {get; init;}
    [JsonPropertyName("name")]
    public string Name {get; init;}
}