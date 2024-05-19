namespace NadekoBot;

public interface INadekoInteractionService
{
    public NadekoInteraction Create(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true);

    public NadekoInteraction Create<T>(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, T, Task> onTrigger,
        in T state,
        bool singleUse = true);

    NadekoInteraction Create(
        ulong userId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true);
}