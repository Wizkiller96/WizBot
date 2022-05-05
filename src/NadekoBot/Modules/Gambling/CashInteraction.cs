#nullable disable
namespace NadekoBot.Modules.Gambling;

public class CashInteraction
{
    public static NadekoInteractionData Data =
        new NadekoInteractionData(new Emoji("🏦"), "cash:bank_show_balance");

    public static NadekoInteraction CreateInstance(
        DiscordSocketClient client,
        ulong userId,
        Func<SocketMessageComponent, Task> action)
        => new NadekoInteractionBuilder()
           .WithData(Data)
           .WithAction(action)
           .Build(client, userId);
}