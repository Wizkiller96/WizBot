﻿using Microsoft.Extensions.DependencyInjection;

public sealed class GuildContextAdapter : GuildContext
{
    private readonly IServiceProvider _services;
    private readonly ICommandContext _ctx;
    private readonly Lazy<IEmbedBuilderService> _ebs;
    private readonly Lazy<IBotStrings> _botStrings;
    private readonly Lazy<ILocalization> _localization;
    
    public override IMedusaStrings Strings { get; }
    public override IGuild Guild { get; }
    public override ITextChannel Channel { get; }
    public override IUserMessage Message
        => _ctx.Message;

    public override IGuildUser User { get; } 

    public override IEmbedBuilder Embed()
        => _ebs.Value.Create();

    public GuildContextAdapter(ICommandContext ctx, IMedusaStrings strings, IServiceProvider services)
    {
        if (ctx.Guild is not IGuild guild || ctx.Channel is not ITextChannel channel)
        {
            throw new ArgumentException("Can't use non-guild context to create GuildContextAdapter", nameof(ctx));
        }

        Strings = strings;
        User = (IGuildUser)ctx.User;

        _services = services;
        _ebs = new(_services.GetRequiredService<IEmbedBuilderService>());
        _botStrings = new(_services.GetRequiredService<IBotStrings>);
        _localization = new(_services.GetRequiredService<ILocalization>());

        (_ctx, Guild, Channel) = (ctx, guild, channel);
    }

    public override string GetText(string key, object[]? args = null)
    {
        args ??= Array.Empty<object>();
        
        var cultureInfo = _localization.Value.GetCultureInfo(_ctx.Guild.Id);
        var output = Strings.GetText(key, cultureInfo, args);
        if (!string.IsNullOrWhiteSpace(output))
            return output;
        
        return _botStrings.Value.GetText(key, cultureInfo, args);
    }
}