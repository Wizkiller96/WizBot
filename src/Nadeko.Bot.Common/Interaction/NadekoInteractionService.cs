namespace NadekoBot;

public class NadekoInteractionService : INadekoInteractionService, INService
{
    private readonly DiscordSocketClient _client;

    public NadekoInteractionService(DiscordSocketClient client)
    {
        _client = client;
    }

    public NadekoInteraction Create<T>(
        ulong userId,
        SimpleInteraction<T> inter)
        => new NadekoInteraction(_client,
            userId,
            inter.Button,
            inter.TriggerAsync,
            onlyAuthor: true);
}