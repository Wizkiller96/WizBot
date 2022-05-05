namespace NadekoBot.Extensions;

public static class SocketMessageComponentExtensions
{
    public static Task RespondAsync(
        this SocketMessageComponent smc,
        string? plainText,
        Embed? embed = null,
        IReadOnlyCollection<Embed>? embeds = null,
        bool sanitizeAll = false,
        MessageComponent? components = null,
        bool ephemeral = true)
    {
        plainText = sanitizeAll
            ? plainText?.SanitizeAllMentions() ?? ""
            : plainText?.SanitizeMentions() ?? "";

        return smc.RespondAsync(plainText,
            embed: embed,
            embeds: embeds is null
                ? null
                : embeds as Embed[] ?? embeds.ToArray(),
            components: components,
            ephemeral: ephemeral,
            options: new()
            {
                RetryMode = RetryMode.AlwaysRetry
            });
    }

    public static Task RespondAsync(
        this SocketMessageComponent smc,
        SmartText text,
        bool sanitizeAll = false,
        bool ephemeral = true)
        => text switch
        {
            SmartEmbedText set => smc.RespondAsync(set.PlainText,
                set.GetEmbed().Build(),
                sanitizeAll: sanitizeAll,
                ephemeral: ephemeral),
            SmartPlainText st => smc.RespondAsync(st.Text,
                default(Embed),
                sanitizeAll: sanitizeAll,
                ephemeral: ephemeral),
            SmartEmbedTextArray arr => smc.RespondAsync(arr.Content,
                embeds: arr.GetEmbedBuilders().Map(e => e.Build()),
                ephemeral: ephemeral),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static Task EmbedAsync(
        this SocketMessageComponent smc,
        IEmbedBuilder? embed,
        string plainText = "",
        IReadOnlyCollection<IEmbedBuilder>? embeds = null,
        NadekoInteraction? inter = null,
        bool ephemeral = false)
        => smc.RespondAsync(plainText,
            embed: embed?.Build(),
            embeds: embeds?.Map(x => x.Build()));
    
    public static Task RespondAsync(
        this SocketMessageComponent ch,
        IEmbedBuilderService eb,
        string text,
        MessageType type,
        bool ephemeral = false,
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

        return ch.EmbedAsync(builder, inter: inter, ephemeral: ephemeral);
    }
    
    // embed title and optional footer overloads

    public static Task RespondErrorAsync(
        this SocketMessageComponent smc,
        IEmbedBuilderService eb,
        string text,
        bool ephemeral = false)
        => smc.RespondAsync(eb, text, MessageType.Error, ephemeral);

    public static Task RespondConfirmAsync(
        this SocketMessageComponent smc,
        IEmbedBuilderService eb,
        string text,
        bool ephemeral = false)
        => smc.RespondAsync(eb, text, MessageType.Ok, ephemeral);
}