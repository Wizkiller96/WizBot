namespace WizBot;

public class WizBotInteractionService : IWizBotInteractionService, INService
{
    private readonly DiscordSocketClient _client;

    public WizBotInteractionService(DiscordSocketClient client)
    {
        _client = client;
    }

    public WizBotInteraction Create<T>(
        ulong userId,
        SimpleInteraction<T> inter)
        => new WizBotInteraction(_client,
            userId,
            inter.Button,
            inter.TriggerAsync,
            onlyAuthor: true);
}