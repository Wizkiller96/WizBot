namespace NadekoBot.Extensions;

public sealed class ResponseBuilder
{
    private ICommandContext? ctx = null;
    private IMessageChannel? channel = null;
    private Embed? embed = null;
    private string? plainText = null;
    private IReadOnlyCollection<EmbedBuilder>? embeds = null;
    private IUserMessage? msg = null;
    private IUser? user = null;
    private bool sanitizeMentions = true;
    private LocStr? locTxt;
    private object[] locParams = [];
    private bool shouldReply = true;
    private readonly IBotStrings _bs;
    private readonly IEmbedBuilderService _ebs;
    private EmbedBuilder? embedBuilder = null;

    public ResponseBuilder(IBotStrings bs, IEmbedBuilderService ebs)
    {
        _bs = bs;
        _ebs = ebs;
    }

    private MessageReference? CreateMessageReference(IMessageChannel targetChannel)
    {
        if (!shouldReply)
            return null;

        var replyTo = msg ?? ctx?.Message;
        // what message are we replying to
        if (replyTo is null)
            return null;

        // we have to have a channel where we are sending the message in order to know whether we can reply to it
        if (targetChannel.Id != replyTo.Channel.Id)
            return null;

        return new(replyTo.Id,
            replyTo.Channel.Id,
            (replyTo.Channel as ITextChannel)?.GuildId,
            failIfNotExists: false);
    }

    public async Task<IUserMessage> SendAsync()
    {
        var targetChannel = InternalResolveChannel() ?? throw new ArgumentNullException(nameof(channel));
        var msgReference = CreateMessageReference(targetChannel);

        var txt = GetText(locTxt);

        if (sanitizeMentions)
            txt = txt?.SanitizeMentions(true);

        return await targetChannel.SendMessageAsync(
            txt,
            embed: embed ?? embedBuilder?.Build(),
            embeds: embeds?.Map(x => x.Build()),
            components: null,
            allowedMentions: sanitizeMentions ? new(AllowedMentionTypes.Users) : AllowedMentions.All,
            messageReference: msgReference);
    }

    private ulong? InternalResolveGuildId(IMessageChannel? targetChannel)
        => ctx?.Guild?.Id ?? (targetChannel as ITextChannel)?.GuildId;

    private IMessageChannel? InternalResolveChannel()
        => channel ?? ctx?.Channel ?? msg?.Channel;

    private string? GetText(LocStr? locStr)
    {
        var targetChannel = InternalResolveChannel();
        var guildId = InternalResolveGuildId(targetChannel);
        return locStr is LocStr ls ? _bs.GetText(ls.Key, guildId, locParams) : plainText;
    }

    private string GetText(LocStr locStr)
    {
        var targetChannel = InternalResolveChannel();
        var guildId = InternalResolveGuildId(targetChannel);
        return _bs.GetText(locStr.Key, guildId, locStr.Params);
    }

    public ResponseBuilder Text(LocStr str)
    {
        locTxt = str;
        return this;
    }

    public ResponseBuilder Text(SmartText text)
    {
        if (text is SmartPlainText spt)
            plainText = spt.Text;
        else if (text is SmartEmbedText set)
            embed = set.GetEmbed().Build();
        else if (text is SmartEmbedTextArray ser)
            embeds = ser.GetEmbedBuilders();

        return this;
    }

    private ResponseBuilder InternalColoredText(string text, EmbedColor color)
    {
        embed = new EmbedBuilder()
                .WithColor(color)
                .WithDescription(text)
                .Build();

        return this;
    }

    private EmbedBuilder CreateEmbedInternal(
        string? title,
        string? text,
        string? url,
        string? footer = null)
    {
        var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription(text);

        if (!string.IsNullOrWhiteSpace(url))
            embed = embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed = embed.WithFooter(footer);

        return embed;
    }

    private EmbedBuilder PaintEmbedInternal(EmbedBuilder eb, EmbedColor color)
        => color switch
        {
            EmbedColor.Ok => eb.WithOkColor(),
            EmbedColor.Pending => eb.WithPendingColor(),
            EmbedColor.Error => eb.WithErrorColor(),
        };

    public ResponseBuilder Error(
        string? title,
        string? text,
        string? url = null,
        string? footer = null)
    {
        var eb = CreateEmbedInternal(title, text, url, footer);
        embed = PaintEmbedInternal(eb, EmbedColor.Error).Build();
        return this;
    }


    public ResponseBuilder Confirm(
        string? title,
        string? text,
        string? url = null,
        string? footer = null)
    {
        var eb = CreateEmbedInternal(title, text, url, footer);
        embed = PaintEmbedInternal(eb, EmbedColor.Error).Build();
        return this;
    }

    public ResponseBuilder Confirm(string text)
        => InternalColoredText(text, EmbedColor.Ok);

    public ResponseBuilder Confirm(LocStr str)
        => Confirm(GetText(str));

    public ResponseBuilder Pending(string text)
        => InternalColoredText(text, EmbedColor.Ok);

    public ResponseBuilder Pending(LocStr str)
        => Pending(GetText(str));

    public ResponseBuilder Error(string text)
        => InternalColoredText(text, EmbedColor.Error);

    public ResponseBuilder Error(LocStr str)
        => Error(GetText(str));


    public ResponseBuilder UserBasedMentions()
    {
        sanitizeMentions = !((InternalResolveUser() as IGuildUser)?.GuildPermissions.MentionEveryone ?? false);
        return this;
    }

    private IUser? InternalResolveUser()
        => ctx?.User ?? user ?? msg?.Author;

    public ResponseBuilder Embed(EmbedBuilder eb)
    {
        embedBuilder = eb;
        return this;
    }

    public ResponseBuilder Embed(Func<IEmbedBuilderService, EmbedBuilder> embedFactory)
    {
        // todo colors
        this.embed = embedFactory(_ebs).Build();

        return this;
    }

    public ResponseBuilder Channel(IMessageChannel channel)
    {
        this.channel = channel;
        return this;
    }

    public ResponseBuilder Sanitize(bool shouldSantize = true)
    {
        sanitizeMentions = shouldSantize;
        return this;
    }

    public ResponseBuilder Context(ICommandContext ctx)
    {
        this.ctx = ctx;
        return this;
    }

    public ResponseBuilder Message(IUserMessage msg)
    {
        this.msg = msg;
        return this;
    }

    public ResponseBuilder User(IUser user)
    {
        this.user = user;
        return this;
    }

    public ResponseBuilder NoReply()
    {
        shouldReply = false;
        return this;
    }

    public ResponseBuilder Interaction(NadekoInteraction inter)
    {
        // todo implement
        return this;
    }

    public ResponseBuilder Embeds(IReadOnlyCollection<EmbedBuilder> inputEmbeds)
    {
        embeds = inputEmbeds;
        return this;
    }
}