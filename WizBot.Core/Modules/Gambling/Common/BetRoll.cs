using System;
using System.Linq;

namespace WizBot.Core.Modules.Gambling.Common
{
    public class Betroll
    {
        public class Result
        {
            public int Roll { get; set; }
            public float Multiplier { get; set; }
            public int Threshold { get; set; }
        }


        private readonly IOrderedEnumerable<GamblingConfig.BetRollConfig.Pair> _thresholdPairs;
        private readonly Random _rng;

        public Betroll(GamblingConfig.BetRollConfig settings)
        {
            _thresholdPairs = settings.Pairs.OrderByDescending(x => x.WhenAbove);
            _rng = new Random();
        }

        public Result Roll()
        {
            var roll = _rng.Next(0, 101);

            var pair = _thresholdPairs.FirstOrDefault(x => x.WhenAbove < roll);
            if (pair is null)
            {
                return new Result
                {
                    Multiplier = 0,
                    Roll = roll,
                };
            }

            return new Result
            {
                Multiplier = pair.MultiplyBy,
                Roll = roll,
                Threshold = pair.WhenAbove,
            };
        }
    }
}