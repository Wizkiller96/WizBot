using System;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Configs;
using NadekoBot.Extensions;

namespace NadekoBot.Services
{
    public interface IEmbedBuilderService
    {
        IEmbedBuilder Create(ICommandContext ctx = null);
        IEmbedBuilder Create(EmbedBuilder eb);
    }

    public class EmbedBuilderService : IEmbedBuilderService, INService
    {
        private readonly BotConfigService _botConfigService;

        public EmbedBuilderService(BotConfigService botConfigService)
        {
            _botConfigService = botConfigService;
        }
        
        public IEmbedBuilder Create(ICommandContext ctx = null) 
            => new DiscordEmbedBuilderWrapper(_botConfigService.Data);
        
        public IEmbedBuilder Create(EmbedBuilder embed) 
            => new DiscordEmbedBuilderWrapper(_botConfigService.Data, embed);
    }
    
    public sealed class DiscordEmbedBuilderWrapper : IEmbedBuilder
    {
        private readonly BotConfig _botConfig;
        private EmbedBuilder _embed;

        public DiscordEmbedBuilderWrapper(in BotConfig botConfig, EmbedBuilder embed = null)
        {
            _botConfig = botConfig;
            _embed = embed ?? new EmbedBuilder();
        }

        public IEmbedBuilder WithDescription(string desc)
            => Wrap(_embed.WithDescription(desc));

        public IEmbedBuilder WithTitle(string title)
            => Wrap(_embed.WithTitle(title));

        public IEmbedBuilder AddField(string title, object value, bool isInline = false)
            => Wrap(_embed.AddField(title, value, isInline));

        public IEmbedBuilder WithFooter(string text, string iconUrl = null)
            => Wrap(_embed.WithFooter(text, iconUrl));

        public IEmbedBuilder WithAuthor(string name, string iconUrl = null, string url = null)
            => Wrap(_embed.WithAuthor(name, iconUrl, url));

        public IEmbedBuilder WithUrl(string url)
            => Wrap(_embed.WithUrl(url));

        public IEmbedBuilder WithImageUrl(string url)
            => Wrap(_embed.WithImageUrl(url));

        public IEmbedBuilder WithThumbnailUrl(string url)
            => Wrap(_embed.WithThumbnailUrl(url));
        
        public IEmbedBuilder WithColor(EmbedColor color)
            => color switch
            {
                EmbedColor.Ok => Wrap(_embed.WithColor(_botConfig.Color.Ok.ToDiscordColor())),
                EmbedColor.Pending => Wrap(_embed.WithColor(_botConfig.Color.Pending.ToDiscordColor())),
                EmbedColor.Error => Wrap(_embed.WithColor(_botConfig.Color.Error.ToDiscordColor())),
                _ => throw new ArgumentOutOfRangeException(nameof(color), "Unsupported EmbedColor type")
            };

        public Embed Build()
            => _embed.Build();

        private IEmbedBuilder Wrap(EmbedBuilder eb)
        {
            _embed = eb;
            return this;
        }
    }
}