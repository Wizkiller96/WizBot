namespace NadekoBot.Extensions;

public static class MessageChannelExtensions
{
    private static readonly IEmote _arrowLeft = new Emoji("⬅");
    private static readonly IEmote _arrowRight = new Emoji("➡");

    public static Task<IUserMessage> EmbedAsync(this IMessageChannel ch, IEmbedBuilder embed, string msg = "")
        => ch.SendMessageAsync(msg,
            embed: embed.Build(),
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });

    public static Task<IUserMessage> SendAsync(
        this IMessageChannel channel,
        string? plainText,
        Embed? embed,
        bool sanitizeAll = false)
    {
        plainText = sanitizeAll ? plainText?.SanitizeAllMentions() ?? "" : plainText?.SanitizeMentions() ?? "";

        return channel.SendMessageAsync(plainText, embed: embed);
    }

    public static Task<IUserMessage> SendAsync(this IMessageChannel channel, SmartText text, bool sanitizeAll = false)
        => text switch
        {
            SmartEmbedText set => channel.SendAsync(set.PlainText, set.GetEmbed().Build(), sanitizeAll),
            SmartPlainText st => channel.SendAsync(st.Text, null, sanitizeAll),
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

    /// <summary>
    ///     danny kamisama
    /// </summary>
    public static async Task SendPaginatedConfirmAsync(
        this ICommandContext ctx,
        int currentPage,
        Func<int, Task<IEmbedBuilder>> pageFunc,
        int totalElements,
        int itemsPerPage,
        bool addPaginatedFooter = true)
    {
        var embed = await pageFunc(currentPage);

        var lastPage = (totalElements - 1) / itemsPerPage;

        var canPaginate = true;
        if (ctx.Guild is SocketGuild sg && !sg.CurrentUser.GetPermissions((IGuildChannel)ctx.Channel).AddReactions)
            canPaginate = false;

        if (!canPaginate)
            embed.WithFooter("⚠️ AddReaction permission required for pagination.");
        else if (addPaginatedFooter)
            embed.AddPaginatedFooter(currentPage, lastPage);

        var msg = await ctx.Channel.EmbedAsync(embed);

        if (lastPage == 0 || !canPaginate)
            return;

        await msg.AddReactionAsync(_arrowLeft);
        await msg.AddReactionAsync(_arrowRight);

        await Task.Delay(2000);

        var lastPageChange = DateTime.MinValue;

        async Task ChangePage(SocketReaction r)
        {
            try
            {
                if (r.UserId != ctx.User.Id)
                    return;
                if (DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                    return;
                if (r.Emote.Name == _arrowLeft.Name)
                {
                    if (currentPage == 0)
                        return;
                    lastPageChange = DateTime.UtcNow;
                    var toSend = await pageFunc(--currentPage);
                    if (addPaginatedFooter)
                        toSend.AddPaginatedFooter(currentPage, lastPage);
                    await msg.ModifyAsync(x => x.Embed = toSend.Build());
                }
                else if (r.Emote.Name == _arrowRight.Name)
                {
                    if (lastPage > currentPage)
                    {
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(++currentPage);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build());
                    }
                }
            }
            catch (Exception)
            {
                //ignored
            }
        }

        using (msg.OnReaction((DiscordSocketClient)ctx.Client, ChangePage, ChangePage))
        {
            await Task.Delay(30000);
        }

        try
        {
            if (msg.Channel is ITextChannel && ((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                await msg.RemoveAllReactionsAsync();
            else
            {
                await msg.Reactions.Where(x => x.Value.IsMe)
                         .Select(x => msg.RemoveReactionAsync(x.Key, ctx.Client.CurrentUser))
                         .WhenAll();
            }
        }
        catch
        {
            // ignored
        }
    }

    public static Task OkAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("✅"));

    public static Task ErrorAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("❌"));

    public static Task WarningAsync(this ICommandContext ctx)
        => ctx.Message.AddReactionAsync(new Emoji("⚠️"));
}