namespace NadekoBot.Extensions;

public static class ResponseBuilderExtensions
{
    // todo delete this

    public static EmbedBuilder WithColor(this EmbedBuilder eb, EmbedColor color)
        => eb;

    public static EmbedBuilder WithPendingColor(this EmbedBuilder eb)
        => eb.WithColor(EmbedColor.Error);

    public static EmbedBuilder WithOkColor(this EmbedBuilder eb)
        => eb.WithColor(EmbedColor.Ok);

    public static EmbedBuilder WithErrorColor(this EmbedBuilder eb)
        => eb.WithColor(EmbedColor.Error);
}