﻿using Discord;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using Wof = WizBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortuneGame;
using WizBot.Modules.Gambling.Services;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Common;
using System.Collections.Immutable;
using WizBot.Core.Modules.Gambling.Services;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
        {
            private static readonly ImmutableArray<string> _emojis = new string[] {
            "⬆",
            "↖",
            "⬅",
            "↙",
            "⬇",
            "↘",
            "➡",
            "↗" }.ToImmutableArray();

            private readonly ICurrencyService _cs;
            private readonly DbService _db;

            public WheelOfFortuneCommands(ICurrencyService cs, DbService db, GamblingConfigService configService)
                : base(configService)
            {
                _cs = cs;
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task WheelOfFortune(ShmartNumber amount)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;

                if (!await _cs.RemoveAsync(ctx.User.Id, "Wheel Of Fortune - bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync("not_enough", CurrencySign).ConfigureAwait(false);
                    return;
                }

                var result = await _service.WheelOfFortuneSpinAsync(ctx.User.Id, amount).ConfigureAwait(false);

                await ctx.Channel.SendConfirmAsync(
Format.Bold($@"{ctx.User.ToString()} won: {result.Amount + CurrencySign}

   『{Wof.Multipliers[1]}』   『{Wof.Multipliers[0]}』   『{Wof.Multipliers[7]}』

『{Wof.Multipliers[2]}』      {_emojis[result.Index]}      『{Wof.Multipliers[6]}』

     『{Wof.Multipliers[3]}』   『{Wof.Multipliers[4]}』   『{Wof.Multipliers[5]}』")).ConfigureAwait(false);
            }
        }
    }
}