namespace NadekoBot;

public static class EmbedBuilderExtensions
{
    public static IEmbedBuilder WithOkColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Ok);
    
    public static IEmbedBuilder WithPendingColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Pending);
    
    public static IEmbedBuilder WithErrorColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Error);
    
}