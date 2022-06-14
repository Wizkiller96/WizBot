namespace NadekoBot.Modules.Help;

public class DonateSelfhostingInteraction : NInteraction
{
    protected override NadekoInteractionData Data
        => new NadekoInteractionData(new Emoji("🖥️"), "donate:selfhosting", "Selfhosting");
    
    public DonateSelfhostingInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}