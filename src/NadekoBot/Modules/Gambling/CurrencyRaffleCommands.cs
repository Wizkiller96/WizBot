using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using System.Linq;
using Discord.Commands;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Common;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyRaffleCommands : GamblingSubmodule<CurrencyRaffleService>
        {
            public enum Mixed { Mixed }

            public CurrencyRaffleCommands(GamblingConfigService gamblingConfService) : base(gamblingConfService)
            {
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public Task RaffleCur(Mixed _, ShmartNumber amount) =>
                RaffleCur(amount, true);

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task RaffleCur(ShmartNumber amount, bool mixed = false)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;
                async Task OnEnded(IUser arg, long won)
                {
                    await SendConfirmAsync(GetText(strs.rafflecur_ended(CurrencyName, Format.Bold(arg.ToString()), won + CurrencySign)));
                }
                var res = await _service.JoinOrCreateGame(ctx.Channel.Id,
                    ctx.User, amount, mixed, OnEnded)
                        .ConfigureAwait(false);

                if (res.Item1 != null)
                {
                    await SendConfirmAsync(GetText(strs.rafflecur(res.Item1.GameType.ToString())),
                        string.Join("\n", res.Item1.Users.Select(x => $"{x.DiscordUser} ({x.Amount})")),
                        footer: GetText(strs.rafflecur_joined(ctx.User.ToString())));
                }
                else
                {
                    if (res.Item2 == CurrencyRaffleService.JoinErrorType.AlreadyJoinedOrInvalidAmount)
                        await ReplyErrorLocalizedAsync(strs.rafflecur_already_joined).ConfigureAwait(false);
                    else if (res.Item2 == CurrencyRaffleService.JoinErrorType.NotEnoughCurrency)
                        await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                }
            }
        }
    }
}
