﻿using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Services;

namespace WizBot.Modules.Gambling.Common.WheelOfFortune
{
    public class WheelOfFortuneGame
    {
        public class Result
        {
            public int Index { get; set; }
            public long Amount { get; set; }
        }

        private readonly WizBotRandom _rng;
        private readonly ICurrencyService _cs;
        private readonly long _bet;
        private readonly GamblingConfig _config;
        private readonly ulong _userId;

        public WheelOfFortuneGame(ulong userId, long bet, GamblingConfig config, ICurrencyService cs)
        {
            _rng = new WizBotRandom();
            _cs = cs;
            _bet = bet;
            _config = config;
            _userId = userId;
        }

        public async Task<Result> SpinAsync()
        {
            var result = _rng.Next(0, _config.WheelOfFortune.Multipliers.Length);

            var amount = (long)(_bet * _config.WheelOfFortune.Multipliers[result]);

            if (amount > 0)
                await _cs.AddAsync(_userId, "Wheel Of Fortune - won", amount, gamble: true).ConfigureAwait(false);

            return new Result
            {
                Index = result,
                Amount = amount,
            };
        }
    }
}