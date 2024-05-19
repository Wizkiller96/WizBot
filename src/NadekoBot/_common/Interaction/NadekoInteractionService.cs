namespace NadekoBot;

public class NadekoInteractionService : INadekoInteractionService, INService
{
    private readonly DiscordSocketClient _client;

    public NadekoInteractionService(DiscordSocketClient client)
    {
        _client = client;
    }

    public NadekoInteraction Create(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true)
        => new NadekoButtonInteraction(_client,
            userId,
            button,
            onTrigger,
            onlyAuthor: true,
            singleUse: singleUse);

    public NadekoInteraction Create<T>(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, T, Task> onTrigger,
        in T state,
        bool singleUse = true)
        => Create(userId,
            button,
            ((Func<T, Func<SocketMessageComponent, Task>>)((data)
                => smc => onTrigger(smc, data)))(state),
            singleUse);
    
    public NadekoInteraction Create(
        ulong userId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true)
        => new NadekoSelectInteraction(_client,
            userId,
            menu,
            onTrigger,
            onlyAuthor: true,
            singleUse: singleUse);
}