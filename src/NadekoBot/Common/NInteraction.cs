namespace NadekoBot.Common;

public abstract class NInteraction
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _userId;
    private readonly Func<SocketMessageComponent, Task> _action;

    protected abstract NadekoInteractionData Data { get; }

    public NInteraction(
        DiscordSocketClient client,
        ulong userId,
        Func<SocketMessageComponent, Task> action)
    {
        _client = client;
        _userId = userId;
        _action = action;
    }

    public NadekoButtonInteraction GetInteraction()
        => new NadekoInteractionBuilder()
           .WithData(Data)
           .WithAction(_action)
           .Build(_client, _userId);
}