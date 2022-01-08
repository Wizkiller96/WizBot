#nullable disable
using NadekoBot.Common.Configs;

namespace NadekoBot.Services;

public interface IEmbedBuilderService
{
    IEmbedBuilder Create(ICommandContext ctx = null);
    IEmbedBuilder Create(EmbedBuilder eb);
}

public class EmbedBuilderService : IEmbedBuilderService, INService
{
    private readonly BotConfigService _botConfigService;

    public EmbedBuilderService(BotConfigService botConfigService)
        => _botConfigService = botConfigService;

    public IEmbedBuilder Create(ICommandContext ctx = null)
        => new DiscordEmbedBuilderWrapper(_botConfigService.Data);

    public IEmbedBuilder Create(EmbedBuilder embed)
        => new DiscordEmbedBuilderWrapper(_botConfigService.Data, embed);
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

    public IEmbedBuilder WithDescription(string desc)
        => Wrap(embed.WithDescription(desc));

    public IEmbedBuilder WithTitle(string title)
        => Wrap(embed.WithTitle(title));

    public IEmbedBuilder AddField(string title, object value, bool isInline = false)
        => Wrap(embed.AddField(title, value, isInline));

    public IEmbedBuilder WithFooter(string text, string iconUrl = null)
        => Wrap(embed.WithFooter(text, iconUrl));

    public IEmbedBuilder WithAuthor(string name, string iconUrl = null, string url = null)
        => Wrap(embed.WithAuthor(name, iconUrl, url));

    public IEmbedBuilder WithUrl(string url)
        => Wrap(embed.WithUrl(url));

    public IEmbedBuilder WithImageUrl(string url)
        => Wrap(embed.WithImageUrl(url));

    public IEmbedBuilder WithThumbnailUrl(string url)
        => Wrap(embed.WithThumbnailUrl(url));

    public IEmbedBuilder WithColor(EmbedColor color)
        => color switch
        {
            EmbedColor.Ok => Wrap(embed.WithColor(_botConfig.Color.Ok.ToDiscordColor())),
            EmbedColor.Pending => Wrap(embed.WithColor(_botConfig.Color.Pending.ToDiscordColor())),
            EmbedColor.Error => Wrap(embed.WithColor(_botConfig.Color.Error.ToDiscordColor())),
            _ => throw new ArgumentOutOfRangeException(nameof(color), "Unsupported EmbedColor type")
        };

    public Embed Build()
        => embed.Build();

    private IEmbedBuilder Wrap(EmbedBuilder eb)
    {
        embed = eb;
        return this;
    }
}