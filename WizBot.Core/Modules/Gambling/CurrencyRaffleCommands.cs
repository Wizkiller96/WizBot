using WizBot.Common.Attributes;
using WizBot.Core.Modules.Gambling.Services;
using System.Threading.Tasks;
using Discord;
using System;
using WizBot.Core.Services;
using WizBot.Extensions;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyRaffleCommands : WizBotSubmodule<CurrencyRaffleService>
        {
            private readonly IBotConfigProvider _bc;

            public CurrencyRaffleCommands(IBotConfigProvider bc)
            {
                _bc = bc;
            }
            [WizBotCommand, Usage, Description, Aliases]
            public async Task RaffleCur(int amount)
            {
                async Task OnEnded(IUser arg, int won)
                {
                    await ReplyConfirmLocalized("rafflecur_ended", _bc.BotConfig.CurrencyName, Format.Bold(arg.ToString()), won + _bc.BotConfig.CurrencySign);
                }
                var res = await _service.JoinOrCreateGame(Context.Channel.Id,
                    Context.User, amount, OnEnded)
                        .ConfigureAwait(false);

                if (res.Item1 != null)
                {
                    await Context.Channel.SendConfirmAsync(GetText("rafflecur_joined", Context.User.ToString()),
                        string.Join("\n", res.Item1.Users)).ConfigureAwait(false);
                }
                else
                {
                    if (res.Item2 == CurrencyRaffleService.JoinErrorType.AlreadyJoined)
                        await ReplyErrorLocalized("rafflecur_already_joined").ConfigureAwait(false);
                    else if (res.Item2 == CurrencyRaffleService.JoinErrorType.NotEnoughCurrency)
                        await ReplyErrorLocalized("not_enough").ConfigureAwait(false);
                }
            }
        }
    }
}