using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class Message
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}