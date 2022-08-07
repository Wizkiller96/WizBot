namespace NadekoBot;

public interface INadekoInteractionService
{
    public NadekoInteraction Create<T>(
        ulong userId,
        SimpleInteraction<T> inter);
}