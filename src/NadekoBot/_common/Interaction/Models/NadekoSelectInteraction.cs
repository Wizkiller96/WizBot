namespace NadekoBot;

public sealed class NadekoSelectInteraction : NadekoInteraction
{
    public NadekoSelectInteraction(
        DiscordSocketClient client,
        ulong authorId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onClick,
        bool onlyAuthor,
        bool singleUse = true)
        : base(client, authorId, menu.CustomId, onClick, onlyAuthor, singleUse)
    {
        Menu = menu;
    }

    public SelectMenuBuilder Menu { get; }

    public override void AddTo(ComponentBuilder cb)
        => cb.WithSelectMenu(Menu);
}