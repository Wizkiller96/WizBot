using System;
using System.Linq;

namespace NadekoBot.Modules.Gambling.Common
{
    public class Betroll
    {
        public class Result
        {
            public int Roll { get; set; }
            public float Multiplier { get; set; }
            public int Threshold { get; set; }
        }


        private readonly IOrderedEnumerable<BetRollPair> _thresholdPairs;
        private readonly Random _rng;
        
        public Betroll(BetRollConfig settings)
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