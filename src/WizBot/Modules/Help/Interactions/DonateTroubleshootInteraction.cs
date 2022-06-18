namespace WizBot.Modules.Help;

public class DonateTroubleshootInteraction : NInteraction
{
    protected override WizBotInteractionData Data
        => new WizBotInteractionData(new Emoji("❓"), "donate:troubleshoot", "Troubleshoot");
    
    public DonateTroubleshootInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}