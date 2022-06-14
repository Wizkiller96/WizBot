#nullable disable
namespace NadekoBot.Modules.Gambling;

public class CashInteraction : NInteraction
{
    protected override NadekoInteractionData Data
        => new NadekoInteractionData(new Emoji("🏦"), "cash:bank_show_balance");

    public CashInteraction(DiscordSocketClient client, ulong userId, Func<SocketMessageComponent, Task> action)
        : base(client, userId, action)
    {
    }
}