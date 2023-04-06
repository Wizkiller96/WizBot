using Nadeko.Bot.Common;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling;

// todo do we need both currencyprovider and currencyservice
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