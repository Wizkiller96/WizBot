﻿using Discord;

namespace WizBot;

public interface IEmbedBuilder
{
    IEmbedBuilder WithDescription(string? desc);
    IEmbedBuilder WithTitle(string? title);
    IEmbedBuilder AddField(string title, object value, bool isInline = false);
    IEmbedBuilder WithFooter(string text, string? iconUrl = null);
    IEmbedBuilder WithAuthor(string name, string? iconUrl = null, string? url = null);
    IEmbedBuilder WithColor(EmbedColor color);
    IEmbedBuilder WithDiscordColor(Color color);
    Embed Build();
    IEmbedBuilder WithUrl(string url);
    IEmbedBuilder WithImageUrl(string url);
    IEmbedBuilder WithThumbnailUrl(string url);
}