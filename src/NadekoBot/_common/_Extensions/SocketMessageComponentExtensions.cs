
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
            ephemeral: ephemeral);
    }

    public static Task RespondAsync(
        this SocketMessageComponent smc,
        SmartText text,
        bool sanitizeAll = false,
        bool ephemeral = true)
        => text switch
        {
            SmartEmbedText set => smc.RespondAsync(set.PlainText,
                set.IsValid ? set.GetEmbed().Build() : null,
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
        EmbedBuilder? embed,
        string plainText = "",
        IReadOnlyCollection<EmbedBuilder>? embeds = null,
        NadekoInteraction? inter = null,
        bool ephemeral = false)
        => smc.RespondAsync(plainText,
            embed: embed?.Build(),
            embeds: embeds?.Map(x => x.Build()),
            ephemeral: ephemeral);
    
    public static Task RespondAsync(
        this SocketMessageComponent ch,
        IMessageSenderService sender,
        string text,
        MsgType type,
        bool ephemeral = false,
        NadekoInteraction? inter = null)
    {
        var embed = new EmbedBuilder().WithDescription(text);

        embed = (type switch
        {
            MsgType.Error => embed.WithErrorColor(),
            MsgType.Ok => embed.WithOkColor(),
            MsgType.Pending => embed.WithPendingColor(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        });

        return sender.Response(ch)
                     .Embed(embed)
                     .Interaction(inter)
                     .SendAsync(ephemeral: ephemeral);
    }
    
    // embed title and optional footer overloads

    public static Task RespondConfirmAsync(
        this SocketMessageComponent smc,
        IMessageSenderService sender,
        string text,
        bool ephemeral = false)
        => smc.RespondAsync(sender, text, MsgType.Ok, ephemeral);
}