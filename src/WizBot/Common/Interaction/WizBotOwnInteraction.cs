namespace WizBot;

/// <summary>
/// Interaction which only the author can use
/// </summary>
public abstract class WizBotButtonOwnInteraction : WizBotButtonInteraction
{
    protected readonly ulong _authorId;

    protected WizBotButtonOwnInteraction(DiscordSocketClient client, ulong authorId) : base(client)
        => _authorId = authorId;

    protected override ValueTask<bool> Validate(SocketMessageComponent smc)
        => new(smc.User.Id == _authorId);
}