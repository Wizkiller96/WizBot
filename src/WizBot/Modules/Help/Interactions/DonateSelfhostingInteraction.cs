namespace WizBot.Modules.Help;

public class DonateSelfhostingInteraction : NInteraction
{
    protected override WizBotInteractionData Data
        => new WizBotInteractionData(new Emoji("🖥️"), "donate:selfhosting", "Selfhosting");
    
    public DonateSelfhostingInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}