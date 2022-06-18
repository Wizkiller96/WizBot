#nullable disable
namespace WizBot.Modules.Gambling;

public class CashInteraction : NInteraction
{
    protected override WizBotInteractionData Data
        => new WizBotInteractionData(new Emoji("🏦"), "cash:bank_show_balance");

    public CashInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}