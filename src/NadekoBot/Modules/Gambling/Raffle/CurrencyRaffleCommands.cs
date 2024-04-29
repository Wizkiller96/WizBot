#nullable disable
using NadekoBot.Common.TypeReaders;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    public partial class CurrencyRaffleCommands : GamblingSubmodule<CurrencyRaffleService>
    {
        public enum Mixed { Mixed }

        public CurrencyRaffleCommands(GamblingConfigService gamblingConfService)
            : base(gamblingConfService)
        {
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public Task RaffleCur(Mixed _, [OverrideTypeReader(typeof(BalanceTypeReader))] long amount)
            => RaffleCur(amount, true);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task RaffleCur([OverrideTypeReader(typeof(BalanceTypeReader))] long amount, bool mixed = false)
        {
            if (!await CheckBetMandatory(amount))
                return;

            async Task OnEnded(IUser arg, long won)
            {
                await Response()
                      .Confirm(GetText(strs.rafflecur_ended(CurrencyName,
                          Format.Bold(arg.ToString()),
                          won + CurrencySign)))
                      .SendAsync();
            }

            var res = await _service.JoinOrCreateGame(ctx.Channel.Id, ctx.User, amount, mixed, OnEnded);

            if (res.Item1 is not null)
            {
                await Response()
                      .Confirm(GetText(strs.rafflecur(res.Item1.GameType.ToString())),
                          string.Join("\n", res.Item1.Users.Select(x => $"{x.DiscordUser} ({N(x.Amount)})")),
                          footer: GetText(strs.rafflecur_joined(ctx.User.ToString())))
                      .SendAsync();
            }
            else
            {
                if (res.Item2 == CurrencyRaffleService.JoinErrorType.AlreadyJoinedOrInvalidAmount)
                    await Response().Error(strs.rafflecur_already_joined).SendAsync();
                else if (res.Item2 == CurrencyRaffleService.JoinErrorType.NotEnoughCurrency)
                    await Response().Error(strs.not_enough(CurrencySign)).SendAsync();
            }
        }
    }
}