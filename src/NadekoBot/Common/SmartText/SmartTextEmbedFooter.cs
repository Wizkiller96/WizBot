#nullable disable
using Newtonsoft.Json;

namespace NadekoBot;

// todo test smarttextembedfooter and smarttextembedauthor

public class SmartTextEmbedFooter
{
    public string Text { get; set; }

    [JsonProperty("icon_url")]
    public string IconUrl { get; set; }
}