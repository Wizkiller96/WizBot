#nullable disable
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
        public partial Task RaffleCur(Mixed _, ShmartNumber amount)
            => RaffleCur(amount, true);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async partial Task RaffleCur(ShmartNumber amount, bool mixed = false)
        {
            if (!await CheckBetMandatory(amount))
                return;

            async Task OnEnded(IUser arg, long won)
            {
                await SendConfirmAsync(GetText(strs.rafflecur_ended(CurrencyName,
                    Format.Bold(arg.ToString()),
                    won + CurrencySign)));
            }

            var res = await _service.JoinOrCreateGame(ctx.Channel.Id, ctx.User, amount, mixed, OnEnded);

            if (res.Item1 is not null)
            {
                await SendConfirmAsync(GetText(strs.rafflecur(res.Item1.GameType.ToString())),
                    string.Join("\n", res.Item1.Users.Select(x => $"{x.DiscordUser} ({N(x.Amount)})")),
                    footer: GetText(strs.rafflecur_joined(ctx.User.ToString())));
            }
            else
            {
                if (res.Item2 == CurrencyRaffleService.JoinErrorType.AlreadyJoinedOrInvalidAmount)
                    await ReplyErrorLocalizedAsync(strs.rafflecur_already_joined);
                else if (res.Item2 == CurrencyRaffleService.JoinErrorType.NotEnoughCurrency)
                    await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
            }
        }
    }
}