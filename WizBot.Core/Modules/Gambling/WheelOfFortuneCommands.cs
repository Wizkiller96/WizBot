using Discord;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using Wof = WizBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortuneGame;
using WizBot.Modules.Gambling.Services;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Common;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
        {
            private readonly ICurrencyService _cs;
            private readonly DbService _db;

            public WheelOfFortuneCommands(ICurrencyService cs, DbService db)
            {
                _cs = cs;
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task WheelOfFortune(ShmartNumber amount)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;

                if (!await _cs.RemoveAsync(Context.User.Id, "Wheel Of Fortune - bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                var wof = new Wof();

                amount = (long)(amount * wof.Multiplier);

                if (amount > 0)
                    await _cs.AddAsync(Context.User.Id, "Wheel Of Fortune - won", amount, gamble: true).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync(
Format.Bold($@"{Context.User.ToString()} won: {amount + Bc.BotConfig.CurrencySign}

   『{Wof.Multipliers[1]}』   『{Wof.Multipliers[0]}』   『{Wof.Multipliers[7]}』

『{Wof.Multipliers[2]}』      {wof.Emoji}      『{Wof.Multipliers[6]}』

     『{Wof.Multipliers[3]}』   『{Wof.Multipliers[4]}』   『{Wof.Multipliers[5]}』")).ConfigureAwait(false);
            }

            //[WizBotCommand, Usage, Description, Aliases]
            //[RequireContext(ContextType.Guild)]
            //public async Task WofTest(int length = 1000)
            //{
            //    var mults = new Dictionary<float, int>();
            //    for (int i = 0; i < length; i++)
            //    {
            //        var x = new Wof();
            //        if (mults.ContainsKey(x.Multiplier))
            //            ++mults[x.Multiplier];
            //        else
            //            mults.Add(x.Multiplier, 1);
            //    }

            //    var payout = mults.Sum(x => x.Key * x.Value);
            //    await Context.Channel.SendMessageAsync($"Total bet: {length}\n" +
            //        $"Paid out: {payout}\n" +
            //        $"Total Payout: {payout / length:F3}x")
            //        .ConfigureAwait(false);
            //}
        }
    }
}