namespace NadekoBot;

public sealed class NadekoButtonInteraction : NadekoInteraction
{
    public NadekoButtonInteraction(
        DiscordSocketClient client,
        ulong authorId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onAction,
        bool onlyAuthor,
        bool singleUse = true)
        : base(client, authorId, button.CustomId, onAction, onlyAuthor, singleUse)
    {
        Button = button;
    }

    public ButtonBuilder Button { get; }

    public override void AddTo(ComponentBuilder cb)
        => cb.WithButton(Button);
}