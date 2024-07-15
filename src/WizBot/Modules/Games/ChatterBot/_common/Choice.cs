using System.Text.Json.Serialization;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class Choice
{
    [JsonPropertyName("message")]
    public Message Message { get; init; }
}