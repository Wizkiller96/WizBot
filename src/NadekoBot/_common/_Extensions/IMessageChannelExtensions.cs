namespace NadekoBot.Extensions;

public static class MessageChannelExtensions
{
    // main overload that all other send methods reduce to
    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false,
        MessageComponent? components = null,
        IUserMessage? replyTo = null)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        var msgReference = CreateMessageReference(channel, replyTo);
        return channel.SendMessageAsync(plainText,
            embed: embed,
            embeds: embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray(),
            components: components,
            messageReference: msgReference);
    }

    private static MessageReference? CreateMessageReference(IChannel source, IMessage? replyTo)
    {
        if (replyTo is null)
            return null;

        if (replyTo.Channel.Id != source.Id)
            return null;
        
        return new(replyTo.Id,
            replyTo.Channel.Id,
            (replyTo.Channel as ITextChannel)?.GuildId,
            failIfNotExists: false);
    }

    public static async Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        NadekoInteraction? inter,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false,
        IUserMessage? replyTo = null)
    {
        var msg = await channel.SendAsync(plainText,
            embed,
            embeds,
            sanitizeAll,
            inter?.CreateComponent(),
            replyTo);

        if (inter is not null)
            await inter.RunAsync(msg);

        return msg;
    }

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        SmartText text,
        bool sanitizeAll = false,
        IUserMessage? replyTo = null)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText,
                set.IsValid ? set.GetEmbed().Build() : null,
                sanitizeAll: sanitizeAll,
                replyTo: replyTo),
            SmartPlainText st => channel.SendAsync(st.Text,
                default(Embed),
                sanitizeAll: sanitizeAll,
                replyTo: replyTo),
            SmartEmbedTextArray arr => channel.SendAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build()),
                sanitizeAll: sanitizeAll,
                replyTo: replyTo),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static Task<IUserMessage> EmbedAsync(
        this IMessageChannel ch,
        IEmbedBuilder? embed,
        string plainText = "",
        IReadOnlyCollection<IEmbedBuilder>? embeds = null,
        NadekoInteraction? inter = null,
        IUserMessage? replyTo = null)
        => ch.SendAsync(plainText,
            inter,
            embed: embed?.Build(),
            embeds: embeds?.Map(x => x.Build()),
            replyTo: replyTo);

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string text,
        MsgType type,
        NadekoInteraction? inter = null,
        IUserMessage? replyTo = null)
    {
        var builder = eb.Create().WithDescription(text);

        builder = (type switch
        {
            MsgType.Error => builder.WithErrorColor(),
            MsgType.Ok => builder.WithOkColor(),
            MsgType.Pending => builder.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        });

        return ch.EmbedAsync(builder, inter: inter, replyTo: replyTo);
    }

    public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MsgType.Ok);

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        MsgType type,
        string? title,
        string text,
        string? url = null,
        string? footer = null)
    {
        var embed = eb.Create()
                      .WithDescription(text)
                      .WithTitle(title);

        if (url is not null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed.WithFooter(footer);

        embed = type switch
        {
            MsgType.Error => embed.WithErrorColor(),
            MsgType.Ok => embed.WithOkColor(),
            MsgType.Pending => embed.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        return ch.EmbedAsync(embed);
    }

    // embed title and optional footer overloads

    public static Task<IUserMessage> SendConfirmAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string? title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MsgType.Ok, title, text, url, footer);

    public static Task<IUserMessage> SendErrorAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MsgType.Error, title, text, url, footer);

    // regular send overloads
    public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MsgType.Error);

    public static Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, IEmbedBuilder> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
        => ctx.SendPaginatedConfirmAsync(currentPage,
            x => Task.FromResult(pageFunc(x)),
            totalElements,
            itemsPerPage,
            addPaginatedFooter);

    private const string BUTTON_LEFT = "BUTTON_LEFT";
    private const string BUTTON_RIGHT = "BUTTON_RIGHT";

    private static readonly IEmote _arrowLeft = Emote.Parse("<:x:1232256519844790302>");
    private static readonly IEmote _arrowRight = Emote.Parse("<:x:1232256515298295838>");

    public static Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
        => ctx.SendPaginatedConfirmAsync(currentPage,
            pageFunc,
            default(Func<int, ValueTask<SimpleInteraction<object>?>>),
            totalElements,
            itemsPerPage,
            addPaginatedFooter);

    public static async Task SendPaginatedConfirmAsync<T>(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        Func<int, ValueTask<SimpleInteraction<T>?>>? interFactory,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
    {
        var lastPage = (totalElements - 1) / itemsPerPage;

        var embed = await pageFunc(currentPage);

        if (addPaginatedFooter)
            embed.AddPaginatedFooter(currentPage, lastPage);

        SimpleInteraction<T>? maybeInter = null;

        async Task<ComponentBuilder> GetComponentBuilder()
        {
            var cb = new ComponentBuilder();

            cb.WithButton(new ButtonBuilder()
                          .WithStyle(ButtonStyle.Primary)
                          .WithCustomId(BUTTON_LEFT)
                          .WithDisabled(lastPage == 0)
                          .WithEmote(_arrowLeft)
                          .WithDisabled(currentPage <= 0));

            if (interFactory is not null)
            {
                maybeInter = await interFactory(currentPage);

                if (maybeInter is not null)
                    cb.WithButton(maybeInter.Button);
            }

            cb.WithButton(new ButtonBuilder()
                          .WithStyle(ButtonStyle.Primary)
                          .WithCustomId(BUTTON_RIGHT)
                          .WithDisabled(lastPage == 0 || currentPage >= lastPage)
                          .WithEmote(_arrowRight));

            return cb;
        }

        async Task UpdatePageAsync(SocketMessageComponent smc)
        {
            var toSend = await pageFunc(currentPage);
            if (addPaginatedFooter)
                toSend.AddPaginatedFooter(currentPage, lastPage);

            var component = (await GetComponentBuilder()).Build();

            await smc.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = toSend.Build();
                x.Components = component;
            });
        }

        var component = (await GetComponentBuilder()).Build();
        var msg = await ctx.Channel.SendAsync(null, embed: embed.Build(), components: component, replyTo: ctx.Message);

        async Task OnInteractionAsync(SocketInteraction si)
        {
            try
            {
                if (si is not SocketMessageComponent smc)
                    return;

                if (smc.Message.Id != msg.Id)
                    return;

                await si.DeferAsync();
                if (smc.User.Id != ctx.User.Id)
                    return;

                if (smc.Data.CustomId == BUTTON_LEFT)
                {
                    if (currentPage == 0)
                        return;

                    --currentPage;
                    _ = UpdatePageAsync(smc);
                }
                else if (smc.Data.CustomId == BUTTON_RIGHT)
                {
                    if (currentPage >= lastPage)
                        return;

                    ++currentPage;
                    _ = UpdatePageAsync(smc);
                }
                else if (maybeInter is { } inter && inter.Button.CustomId == smc.Data.CustomId)
                {
                    await inter.TriggerAsync(smc);
                    _ = UpdatePageAsync(smc);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in pagination: {ErrorMessage}", ex.Message);
            }
        }

        if (lastPage == 0 && interFactory is null)
            return;

        var client = (DiscordSocketClient)ctx.Client;

        client.InteractionCreated += OnInteractionAsync;

        await Task.Delay(30_000);

        client.InteractionCreated -= OnInteractionAsync;

        await msg.ModifyAsync(mp => mp.Components = new ComponentBuilder().Build());
    }

    private static readonly Emoji _okEmoji = new Emoji("✅");
    private static readonly Emoji _warnEmoji = new Emoji("⚠️");
    private static readonly Emoji _errorEmoji = new Emoji("❌");

    public static Task ReactAsync(this ICommandContext ctx, MsgType type)
    {
        var emoji = type switch
        {
            MsgType.Error => _errorEmoji,
            MsgType.Pending => _warnEmoji,
            MsgType.Ok => _okEmoji,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        return ctx.Message.AddReactionAsync(emoji);
    }

    public static Task OkAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MsgType.Ok);

    public static Task ErrorAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MsgType.Error);

    public static Task WarningAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MsgType.Pending);
}