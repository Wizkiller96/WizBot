#nullable disable
using NadekoBot.Common.Configs;

// todo remove
namespace NadekoBot.Services;

public interface IEmbedBuilderService
{
    EmbedBuilder Create(ICommandContext ctx = null);
}

public class EmbedBuilderService : IEmbedBuilderService, INService
{
    private readonly BotConfigService _botConfigService;

    public EmbedBuilderService(BotConfigService botConfigService)
        => _botConfigService = botConfigService;

    public EmbedBuilder Create(ICommandContext ctx = null)
        => new EmbedBuilder();

}

public sealed class DiscordEmbedBuilderWrapper : IEmbedBuilder
{
    private readonly BotConfig _botConfig;
    private EmbedBuilder embed;

    public DiscordEmbedBuilderWrapper(in BotConfig botConfig, EmbedBuilder embed = null)
    {
        _botConfig = botConfig;
        this.embed = embed ?? new EmbedBuilder();
    }

    public EmbedBuilder WithDescription(string desc)
        => Wrap(embed.WithDescription(desc));

    public EmbedBuilder WithTitle(string title)
        => Wrap(embed.WithTitle(title));

    public EmbedBuilder AddField(string title, object value, bool isInline = false)
        => Wrap(embed.AddField(title, value, isInline));

    public EmbedBuilder WithFooter(string text, string iconUrl = null)
        => Wrap(embed.WithFooter(text, iconUrl));

    public EmbedBuilder WithAuthor(string name, string iconUrl = null, string url = null)
        => Wrap(embed.WithAuthor(name, iconUrl, url));

    public EmbedBuilder WithUrl(string url)
        => Wrap(embed.WithUrl(url));

    public EmbedBuilder WithImageUrl(string url)
        => Wrap(embed.WithImageUrl(url));

    public EmbedBuilder WithThumbnailUrl(string url)
        => Wrap(embed.WithThumbnailUrl(url));

    public EmbedBuilder WithColor(EmbedColor color)
        => color switch
        {
            EmbedColor.Ok => Wrap(embed.WithColor(_botConfig.Color.Ok.ToDiscordColor())),
            EmbedColor.Pending => Wrap(embed.WithColor(_botConfig.Color.Pending.ToDiscordColor())),
            EmbedColor.Error => Wrap(embed.WithColor(_botConfig.Color.Error.ToDiscordColor())),
            _ => throw new ArgumentOutOfRangeException(nameof(color), "Unsupported EmbedColor type")
        };

    public EmbedBuilder WithDiscordColor(Color color)
        => Wrap(embed.WithColor(color));

    public Embed Build()
        => embed.Build();

    private EmbedBuilder Wrap(EmbedBuilder eb)
    {
        embed = eb;
        return eb;
    }
}