using Discord;

namespace NadekoBot;

public interface IEmbedBuilder
{
    EmbedBuilder WithDescription(string? desc);
    EmbedBuilder WithTitle(string? title);
    EmbedBuilder AddField(string title, object value, bool isInline = false);
    EmbedBuilder WithFooter(string text, string? iconUrl = null);
    EmbedBuilder WithAuthor(string name, string? iconUrl = null, string? url = null);
    EmbedBuilder WithColor(EmbedColor color);
    EmbedBuilder WithDiscordColor(Color color);
    Embed Build();
    EmbedBuilder WithUrl(string url);
    EmbedBuilder WithImageUrl(string url);
    EmbedBuilder WithThumbnailUrl(string url);
}