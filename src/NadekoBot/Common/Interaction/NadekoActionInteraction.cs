namespace NadekoBot;

public sealed class NadekoActionInteraction : NadekoOwnInteraction
{
    private readonly NadekoInteractionData _data;
    private readonly Func<SocketMessageComponent, Task> _action;

    public NadekoActionInteraction(
        DiscordSocketClient client,
        ulong authorId,
        NadekoInteractionData data,
        Func<SocketMessageComponent, Task> action
    )
        : base(client, authorId)
    {
        _data = data;
        _action = action;
    }

    public override string Name
        => _data.CustomId;
    public override IEmote Emote
        => _data.Emote;

    public override Task ExecuteOnActionAsync(SocketMessageComponent smc)
        => _action(smc);
}