namespace NadekoBot.Modules.Help;

public class DonateTroubleshootInteraction : NInteraction
{
    protected override NadekoInteractionData Data
        => new NadekoInteractionData(new Emoji("❓"), "donate:troubleshoot", "Troubleshoot");
    
    public DonateTroubleshootInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}