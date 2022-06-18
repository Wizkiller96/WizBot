namespace WizBot;

public sealed class WizBotButtonActionInteraction : WizBotButtonOwnInteraction
{
    private readonly WizBotInteractionData _data;
    private readonly Func<SocketMessageComponent, Task> _action;

    public WizBotButtonActionInteraction(
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

    protected override string Name
        => _data.CustomId;
    protected override IEmote Emote
        => _data.Emote;
    protected override string? Text
        => _data.Text;

    public override Task ExecuteOnActionAsync(SocketMessageComponent smc)
        => _action(smc);
}