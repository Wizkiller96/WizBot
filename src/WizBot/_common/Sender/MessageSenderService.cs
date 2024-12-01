namespace WizBot.Extensions;

public sealed class MessageSenderService : IMessageSenderService, INService
{
    private readonly IBotStrings _bs;
    private readonly BotConfigService _bcs;
    private readonly DiscordSocketClient _client;
    private readonly IGuildColorsService _gcs;

    public MessageSenderService(
        IBotStrings bs,
        BotConfigService bcs,
        DiscordSocketClient client,
        IGuildColorsService gcs)
    {
        _bs = bs;
        _bcs = bcs;
        _client = client;
        _gcs = gcs;
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

    public WizBotEmbedBuilder CreateEmbed(ulong? guildId = null)
        => new WizBotEmbedBuilder(_bcs, guildId is { } gid ? _gcs.GetColors(gid) : null);
}

public class WizBotEmbedBuilder : EmbedBuilder
{
    private readonly Color _okColor;
    private readonly Color _errorColor;
    private readonly Color _pendingColor;

    public WizBotEmbedBuilder(BotConfigService bcsData, Colors? guildColors = null)
    {
        var bcColors = bcsData.Data.Color;
        _okColor = guildColors?.Ok ?? bcColors.Ok.ToDiscordColor();
        _errorColor = guildColors?.Error ?? bcColors.Error.ToDiscordColor();
        _pendingColor = guildColors?.Pending ?? bcColors.Pending.ToDiscordColor();
    }

    public EmbedBuilder WithOkColor()
        => WithColor(_okColor);

    public EmbedBuilder WithErrorColor()
        => WithColor(_errorColor);

    public EmbedBuilder WithPendingColor()
        => WithColor(_pendingColor);
}