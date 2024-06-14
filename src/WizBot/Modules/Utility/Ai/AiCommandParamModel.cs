using System.Text.Json.Serialization;

namespace WizBot.Modules.Utility;

public sealed class AiCommandParamModel
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("desc")]
    public required string Desc { get; set; }
}