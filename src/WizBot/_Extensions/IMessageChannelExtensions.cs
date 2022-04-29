namespace WizBot.Extensions;

public static class MessageChannelExtensions
{
    public static Task<IUserMessage> EmbedAsync(
        this IMessageChannel ch,
        IEmbedBuilder embed,
        string msg = "",
        MessageComponent? components = null)
        => ch.SendMessageAsync(msg,
            embed: embed.Build(),
            components: components,
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        Embed? embed = null,
        Embed[]? embeds = null,
        bool sanitizeAll = false)
    {
        plainText = sanitizeAll ? plainText?.SanitizeAllMentions() ?? "" : plainText?.SanitizeMentions() ?? "";

        return channel.SendMessageAsync(plainText, embed: embed, embeds: embeds);
    }

    public static Task<IUserMessage> SendAsync(this IMessageChannel channel, SmartText text, bool sanitizeAll = false)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText, set.GetEmbed().Build(), sanitizeAll: sanitizeAll),
            SmartPlainText st => channel.SendAsync(st.Text, null, sanitizeAll: sanitizeAll),
            SmartEmbedTextArray arr => channel.SendAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build())),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    // this is a huge problem, because now i don't have
    // access to embed builder service
    // as this is an extension of the message channel
    public static Task<IUserMessage> SendErrorAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string error,
        string? url = null,
        string? footer = null)
    {
        var embed = eb.Create().WithErrorColor().WithDescription(error).WithTitle(title);

        if (url is not null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed.WithFooter(footer);

        return ch.SendMessageAsync("", embed: embed.Build());
    }

    public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, IEmbedBuilderService eb, string error)
        => ch.SendMessageAsync("", embed: eb.Create().WithErrorColor().WithDescription(error).Build());

    public static Task<IUserMessage> SendPendingAsync(this IMessageChannel ch, IEmbedBuilderService eb, string message)
        => ch.SendMessageAsync("", embed: eb.Create().WithPendingColor().WithDescription(message).Build());

    public static Task<IUserMessage> SendConfirmAsync(
        this IMessageChannel ch,
        IEmbedBuilderService eb,
        string title,
        string text,
        string? url = null,
        string? footer = null)
    {
        var embed = eb.Create().WithOkColor().WithDescription(text).WithTitle(title);

        if (url is not null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            embed.WithUrl(url);

        if (!string.IsNullOrWhiteSpace(footer))
            embed.WithFooter(footer);

        return ch.SendMessageAsync("", embed: embed.Build());
    }

    public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, IEmbedBuilderService eb, string text)
        => ch.SendMessageAsync("", embed: eb.Create().WithOkColor().WithDescription(text).Build());

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
    
    private static readonly IEmote _arrowLeft = new Emoji("⬅️");
    private static readonly IEmote _arrowRight = new Emoji("➡️");
    
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
                                    .WithStyle(ButtonStyle.Secondary)
                                    .WithCustomId(BUTTON_LEFT)
                                    .WithDisabled(lastPage == 0)
                                    .WithEmote(_arrowLeft))
                        .WithButton(new ButtonBuilder()
                                    .WithStyle(ButtonStyle.Primary)
                                    .WithCustomId(BUTTON_RIGHT)
                                    .WithDisabled(lastPage == 0)
                                    .WithEmote(_arrowRight))
                        .Build();
        
        var msg = await ctx.Channel.EmbedAsync(embed, components: component);
        
        Task OnInteractionAsync(SocketInteraction si)
        {
            _ = Task.Run(async () =>
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

    public static Task OkAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("✅"));

    public static Task ErrorAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("❌"));

    public static Task WarningAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("⚠️"));
}