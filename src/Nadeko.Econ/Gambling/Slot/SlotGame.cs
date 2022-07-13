namespace Nadeko.Econ.Gambling;

public class SlotGame
{
    private static readonly Random _rng = new NadekoRandom();

    public SlotResult Spin(decimal bet)
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

        return new()
        {
            Won = bet * multi,
            Multiplier = multi,
            Rolls = rolls,
        };
    }
}