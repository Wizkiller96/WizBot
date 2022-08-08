namespace WizBot;

public interface IWizBotInteractionService
{
    public WizBotInteraction Create<T>(
        ulong userId,
        SimpleInteraction<T> inter);
}