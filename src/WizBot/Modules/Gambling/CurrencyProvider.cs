using WizBot.Modules.Gambling.Services;

namespace WizBot.Modules.Gambling;

public sealed class CurrencyProvider : ICurrencyProvider, INService
{
    private readonly GamblingConfigService _cs;

    public CurrencyProvider(GamblingConfigService cs)
    {
        _cs = cs;
    }

    public string GetCurrencySign()
        => _cs.Data.Currency.Sign;
}