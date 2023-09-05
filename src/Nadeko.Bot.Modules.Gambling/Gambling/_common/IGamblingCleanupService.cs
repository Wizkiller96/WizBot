namespace Nadeko.Bot.Modules.Gambling.Gambling._Common;

public interface IGamblingCleanupService
{
    Task DeleteWaifus();
    Task DeleteWaifu(ulong userId);
    Task DeleteCurrency();
}