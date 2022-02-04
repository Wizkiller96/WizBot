#nullable disable
namespace NadekoBot.Modules.Gambling.Common;

public class Betroll
{
    private readonly IOrderedEnumerable<BetRollPair> _thresholdPairs;
    private readonly Random _rng;

    public Betroll(BetRollConfig settings)
    {
        _thresholdPairs = settings.Pairs.OrderByDescending(x => x.WhenAbove);
        _rng = new();
    }

    public Result Roll()
    {
        var roll = _rng.Next(0, 101);

        var pair = _thresholdPairs.FirstOrDefault(x => x.WhenAbove < roll);
        if (pair is null)
        {
            return new()
            {
                Multiplier = 0,
                Roll = roll
            };
        }

        return new()
        {
            Multiplier = pair.MultiplyBy,
            Roll = roll,
            Threshold = pair.WhenAbove
        };
    }

    public class Result
    {
        public int Roll { get; set; }
        public float Multiplier { get; set; }
        public int Threshold { get; set; }
    }
}