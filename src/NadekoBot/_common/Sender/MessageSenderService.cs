using NadekoBot.Common.Configs;
using System.Diagnostics.CodeAnalysis;

namespace NadekoBot.Extensions;

public sealed class MessageSenderService : IMessageSenderService, INService
{
    private readonly IBotStrings _bs;
    private readonly BotConfigService _bcs;
    private readonly DiscordSocketClient _client;

    public MessageSenderService(IBotStrings bs, BotConfigService bcs, DiscordSocketClient client)
    {
        _bs = bs;
        _bcs = bcs;
        _client = client;
    }


    public ResponseBuilder Response(IMessageChannel channel)
        => new ResponseBuilder(_bs, _bcs, _client)
            .Channel(channel);

    public ResponseBuilder Response(ICommandContext ctx)
        => new ResponseBuilder(_bs, _bcs, _client)
            .Context(ctx);

    public ResponseBuilder Response(IUser user)
        => new ResponseBuilder(_bs, _bcs, _client)
            .User(user);

    public ResponseBuilder Response(SocketMessageComponent smc)
        => new ResponseBuilder(_bs, _bcs, _client)
            .Channel(smc.Channel);

    public NadekoEmbedBuilder CreateEmbed()
        => new NadekoEmbedBuilder(_bcs);
}

public class NadekoEmbedBuilder : EmbedBuilder
{
    private readonly BotConfig _bc;

    public NadekoEmbedBuilder(BotConfigService bcs)
    {
        _bc = bcs.Data;
    }

    public EmbedBuilder WithOkColor()
        => WithColor(_bc.Color.Ok.ToDiscordColor());

    public EmbedBuilder WithErrorColor()
        => WithColor(_bc.Color.Error.ToDiscordColor());

    public EmbedBuilder WithPendingColor()
        => WithColor(_bc.Color.Pending.ToDiscordColor());
}