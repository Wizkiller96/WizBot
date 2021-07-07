using System;
using Discord;

namespace NadekoBot.Services
{
    public interface IEmbedBuilderProvider
    {
        public IEmbedBuilder Create();
    }

    public interface IEmbedBuilder
    {
        public IEmbedBuilder WithDescription(string desc);
        public IEmbedBuilder WithTitle(string title);
        public IEmbedBuilder AddField(string title, object value, bool isInline = false);
        public IEmbedBuilder WithFooter(string text, string iconUrl = null);
    }

    public class DiscordEmbedBuilderWrapper : IEmbedBuilder
    {
        private EmbedBuilder _embed;

        public DiscordEmbedBuilderWrapper()
        {
            _embed = new EmbedBuilder();
        }

        public IEmbedBuilder WithDescription(string desc)
            => Wrap(_embed.WithDescription(desc));

        public IEmbedBuilder WithTitle(string title)
            => Wrap(_embed.WithTitle(title));

        public IEmbedBuilder AddField(string title, object value, bool isInline = false)
            => Wrap(_embed.AddField(title, value, isInline));

        public IEmbedBuilder WithFooter(string text, string iconUrl = null)
            => Wrap(_embed.WithFooter(text, iconUrl));

        private IEmbedBuilder Wrap(EmbedBuilder eb)
        {
            _embed = eb;
            return this;
        }
    }
}