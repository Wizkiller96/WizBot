#nullable disable warnings
namespace NadekoBot.Modules.Nsfw;

public record UrlReply
{
    public string Error { get; init; }
    public string Url { get; init; }
    public string Rating { get; init; }
    public string Provider { get; init; }
    public List<string> Tags { get; } = new();
}