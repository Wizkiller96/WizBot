namespace WizBot;

public sealed class WizBotButtonSelectInteractionHandler : WizBotInteractionBase
{
    public WizBotButtonSelectInteractionHandler(
        DiscordSocketClient client,
        ulong authorId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onAction,
        bool onlyAuthor,
        bool singleUse = true)
        : base(client, authorId, menu.CustomId, onAction, onlyAuthor, singleUse)
    {
        Menu = menu;
    }

    public SelectMenuBuilder Menu { get; }

    public override void AddTo(ComponentBuilder cb)
        => cb.WithSelectMenu(Menu);
}