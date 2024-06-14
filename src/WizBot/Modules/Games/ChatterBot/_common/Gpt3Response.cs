#nullable disable
using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class OpenAiCompletionResponse
{
    [JsonPropertyName("choices")]
    public Choice[] Choices { get; set; }
    
    [JsonPropertyName("usage")]
    public OpenAiUsageData Usage { get; set; }
}

public class OpenAiUsageData
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
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