#nullable disable
namespace NadekoBot.Modules.Gambling.Common.Slot;

public class SlotGame
{
    private static readonly Random _rng = new NadekoRandom();

    public Result Spin()
    {
        var rolls = new[] { _rng.Next(0, 6), _rng.Next(0, 6), _rng.Next(0, 6) };
        var multi = 0;

        if (rolls.All(x => x == 5))
            multi = 30;
        else if (rolls.All(x => x == rolls[0]))
            multi = 10;
        else if (rolls.Count(x => x == 5) == 2)
            multi = 4;
        else if (rolls.Any(x => x == 5))
            multi = 1;

        return new(multi, rolls);
    }

    public class Result
    {
        public float Multiplier { get; }
        public int[] Rolls { get; }

        public Result(float multiplier, int[] rolls)
        {
            Multiplier = multiplier;
            Rolls = rolls;
        }
    }
}