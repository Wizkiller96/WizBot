namespace WizBot.Extensions;

public static class ResponseBuilderExtensions
{
    public static EmbedBuilder WithPendingColor(this EmbedBuilder eb)
    {
        if (eb is WizBotEmbedBuilder neb)
            return neb.WithPendingColor();

        return eb;
    }

    public static EmbedBuilder WithOkColor(this EmbedBuilder eb)
    {
        if (eb is WizBotEmbedBuilder neb)
            return neb.WithOkColor();

        return eb;
    }

    public static EmbedBuilder WithErrorColor(this EmbedBuilder eb)
    {
        if (eb is WizBotEmbedBuilder neb)
            return neb.WithErrorColor();

        return eb;
    }
}