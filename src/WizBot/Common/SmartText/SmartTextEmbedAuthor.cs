#nullable disable
using Newtonsoft.Json;

namespace WizBot;

public class SmartTextEmbedAuthor
{
    public string Name { get; set; }

    [JsonProperty("icon_url")]
    public string IconUrl { get; set; }

    public string Url { get; set; }
}