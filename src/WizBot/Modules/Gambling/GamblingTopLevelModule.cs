#nullable disable
using WizBot.Modules.Gambling.Services;
using System.Globalization;

namespace WizBot.Modules.Gambling.Common;

public abstract class GamblingModule<TService> : WizBotModule<TService>
{
    protected GamblingConfig Config
        => _lazyConfig.Value;

    protected string CurrencySign
        => Config.Currency.Sign;

    protected string CurrencyName
        => Config.Currency.Name;

    private readonly Lazy<GamblingConfig> _lazyConfig;

    protected GamblingModule(GamblingConfigService gambService)
        => _lazyConfig = new(() => gambService.Data);

    private async Task<bool> InternalCheckBet(long amount)
    {
        if (amount < 1)
            return false;
        if (amount < Config.MinBet)
        {
            await ReplyErrorLocalizedAsync(strs.min_bet_limit(Format.Bold(Config.MinBet.ToString()) + CurrencySign));
            return false;
        }

        if (Config.MaxBet > 0 && amount > Config.MaxBet)
        {
            await ReplyErrorLocalizedAsync(strs.max_bet_limit(Format.Bold(Config.MaxBet.ToString()) + CurrencySign));
            return false;
        }

        return true;
    }

    protected string N<T>(T cur)
        where T : INumber<T>
    {
        var flowersCi = (CultureInfo)Culture.Clone();
        flowersCi.NumberFormat.CurrencySymbol = CurrencySign;
        flowersCi.NumberFormat.CurrencyNegativePattern = 5;
        return cur.ToString("C0", flowersCi);
    }

    protected Task<bool> CheckBetMandatory(long amount)
    {
        if (amount < 1)
            return Task.FromResult(false);
        return InternalCheckBet(amount);
    }

    protected Task<bool> CheckBetOptional(long amount)
    {
        if (amount == 0)
            return Task.FromResult(true);
        return InternalCheckBet(amount);
    }
}

public abstract class GamblingSubmodule<TService> : GamblingModule<TService>
{
    protected GamblingSubmodule(GamblingConfigService gamblingConfService)
        : base(gamblingConfService)
    {
    }
}