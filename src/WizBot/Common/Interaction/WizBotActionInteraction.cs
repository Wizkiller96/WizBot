namespace WizBot;

public sealed class WizBotActionInteraction : WizBotOwnInteraction
{
    private readonly WizBotInteractionData _data;
    private readonly Func<SocketMessageComponent, Task> _action;

    public WizBotActionInteraction(
        DiscordSocketClient client,
        ulong authorId,
        WizBotInteractionData data,
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