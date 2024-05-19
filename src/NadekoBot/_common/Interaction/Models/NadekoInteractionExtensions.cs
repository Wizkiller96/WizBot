namespace NadekoBot;

public static class NadekoInteractionExtensions
{
    public static MessageComponent CreateComponent(
        this NadekoInteraction nadekoInteraction
    )
    {
        var cb = new ComponentBuilder();

        nadekoInteraction.AddTo(cb);

        return cb.Build();
    }
}