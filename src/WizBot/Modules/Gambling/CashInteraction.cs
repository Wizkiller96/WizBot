#nullable disable
namespace WizBot.Modules.Gambling;

public class CashInteraction
{
    public static WizBotInteractionData Data =
        new WizBotInteractionData(new Emoji("🏦"), "cash:bank_show_balance");

    public static WizBotInteraction CreateInstance(
        DiscordSocketClient client,
        ulong userId,
        Func<SocketMessageComponent, Task> action)
        => new WizBotInteractionBuilder()
           .WithData(Data)
           .WithAction(action)
           .Build(client, userId);
}