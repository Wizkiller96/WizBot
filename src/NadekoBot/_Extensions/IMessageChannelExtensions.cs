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
        MessageComponent? components = null)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        return channel.SendMessageAsync(plainText,
            embed: embed,
            embeds: embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray(),
            components: components,
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });
    }

    public static async Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        NadekoInteraction? inter,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false)
    {
        var msg = await channel.SendAsync(plainText,
            embed,
            embeds,
            sanitizeAll,
            inter?.CreateComponent());
        
        if (inter is not null)
            await inter.RunAsync(msg);

        return msg;
    }

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        SmartText text,
        bool sanitizeAll = false)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText,
                set.GetEmbed().Build(),
                sanitizeAll: sanitizeAll),
            SmartPlainText st => channel.SendAsync(st.Text,
                default(Embed),
                sanitizeAll: sanitizeAll),
            SmartEmbedTextArray arr => channel.SendAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build())),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static Task<IUserMessage> EmbedAsync(
        this IMessageChannel ch,
        IEmbedBuilder? embed,
        string plainText = "",
        IReadOnlyCollection<IEmbedBuilder>? embeds = null,
        NadekoInteraction? inter = null)
        => ch.SendAsync(plainText,
            inter,
            embed: embed?.Build(),
            embeds: embeds?.Map(x => x.Build()));
    
    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string text,
        MessageType type,
        NadekoInteraction? inter = null)
    {
        var builder = eb.Create().WithDescription(text);

        builder = (type switch
        {
            MessageType.Error => builder.WithErrorColor(),
            MessageType.Ok => builder.WithOkColor(),
            MessageType.Pending => builder.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        });

        return ch.EmbedAsync(builder, inter: inter);
    }
    
    // regular send overloads
    public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MessageType.Error);
    
    public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendAsync(eb, text, MessageType.Ok);
    
    public static Task<IUserMessage> SendAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        MessageType type,
        string title,
        string text,
        string? url = null,
        string? footer = null)
    {
        var embed = eb.Create().WithDescription(text).WithTitle(title);

        if (url is not null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed.WithFooter(footer);

        embed = type switch
        {
            MessageType.Error => embed.WithErrorColor(),
            MessageType.Ok => embed.WithOkColor(),
            MessageType.Pending => embed.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        
        return ch.EmbedAsync(embed);
    }

    // embed title and optional footer overloads
    
    public static Task<IUserMessage> SendConfirmAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MessageType.Ok, title, text, url, footer);
    
    public static Task<IUserMessage> SendErrorAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string text,
        string? url = null,
        string? footer = null)
        => ch.SendAsync(eb, MessageType.Error, title, text, url, footer);

    // weird stuff
    
    public static Task<IUserMessage> SendTableAsync<T>(
        this IMessageChannel ch,
        string seed,
        IEnumerable<T> items,
        Func<T, string> howToPrint,
        int columns = 3)
        => ch.SendMessageAsync($@"{seed}```css
{items.Chunk(columns)
      .Select(ig => string.Concat(ig.Select(howToPrint)))
      .Join("\n")}
```");

    public static Task<IUserMessage> SendTableAsync<T>(
        this IMessageChannel ch,
        IEnumerable<T> items,
        Func<T, string> howToPrint,
        int columns = 3)
        => ch.SendTableAsync("", items, howToPrint, columns);

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
    
    private static readonly IEmote _arrowLeft = Emote.Parse("<:x:969658061805465651>");
    private static readonly IEmote _arrowRight = Emote.Parse("<:x:969658062220701746>");
    
    public static async Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
    {
        var lastPage = (totalElements - 1) / itemsPerPage;
        
        var embed = await pageFunc(currentPage);

        if (addPaginatedFooter)
            embed.AddPaginatedFooter(currentPage, lastPage);

        var component = new ComponentBuilder()
                        .WithButton(new ButtonBuilder()
                                    .WithStyle(ButtonStyle.Primary)
                                    .WithCustomId(BUTTON_LEFT)
                                    .WithDisabled(lastPage == 0)
                                    .WithEmote(_arrowLeft))
                        .WithButton(new ButtonBuilder()
                                    .WithStyle(ButtonStyle.Primary)
                                    .WithCustomId(BUTTON_RIGHT)
                                    .WithDisabled(lastPage == 0)
                                    .WithEmote(_arrowRight))
                        .Build();
        
        var msg = await ctx.Channel.SendAsync(null, embed: embed.Build(), components: component);
        
        Task OnInteractionAsync(SocketInteraction si)
        {
            _ = Task.Run(async () =>
            {
                if (si is not SocketMessageComponent smc)
                    return;

                if (smc.Message.Id != msg.Id)
                    return;

                if (smc.Data.CustomId != BUTTON_LEFT && smc.Data.CustomId != BUTTON_RIGHT)
                    return;
                
                await si.DeferAsync();
                if (smc.User.Id != ctx.User.Id)
                    return;

                if (smc.Data.CustomId == BUTTON_LEFT)
                {
                    if (currentPage == 0)
                        return;

                    var toSend = await pageFunc(--currentPage);
                    if (addPaginatedFooter)
                        toSend.AddPaginatedFooter(currentPage, lastPage);

                    await smc.ModifyOriginalResponseAsync(x => x.Embed = toSend.Build());
                }
                else if (smc.Data.CustomId == BUTTON_RIGHT)
                {
                    if (lastPage > currentPage)
                    {
                        var toSend = await pageFunc(++currentPage);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);

                        await smc.ModifyOriginalResponseAsync(x => x.Embed = toSend.Build());
                    }
                }
            });

            return Task.CompletedTask;
        }

        if (lastPage == 0)
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
    
    public static Task ReactAsync(this ICommandContext ctx, MessageType type)
    {
        var emoji = type switch
        {
            MessageType.Error => _errorEmoji,
            MessageType.Pending => _warnEmoji,
            MessageType.Ok => _okEmoji,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        return ctx.Message.AddReactionAsync(emoji);
    }

    public static Task OkAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Ok);

    public static Task ErrorAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Error);

    public static Task WarningAsync(this ICommandContext ctx)
        => ctx.ReactAsync(MessageType.Pending);
}

public enum MessageType
{
    Ok,
    Pending,
    Error
}