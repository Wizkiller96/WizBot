namespace Nadeko.Econ.Gambling;

public class SlotGame
{
    private static readonly Random _rng = new NadekoRandom();

    public SlotResult Spin(decimal bet)
    {
        var rolls = new[] { _rng.Next(0, 6), _rng.Next(0, 6), _rng.Next(0, 6) };
        var multi = 0;
        var winType = SlotWinType.None;

        if (rolls.All(x => x == 5))
        {
            winType = SlotWinType.TrippleJoker;
            multi = 30;
        }
        else if (rolls.All(x => x == rolls[0]))
        {
            winType = SlotWinType.TrippleNormal;
            multi = 10;
        }
        else if (rolls.Count(x => x == 5) == 2)
        {
            winType = SlotWinType.DoubleJoker;
            multi = 4;
        }
        else if (rolls.Any(x => x == 5))
        {
            winType = SlotWinType.SingleJoker;
            multi = 1;
        }

        return new()
        {
            Won = bet * multi,
            WinType = winType,
            Multiplier = multi,
            Rolls = rolls,
        };
    }
}

public enum SlotWinType : byte
{
    None,
    SingleJoker,
    DoubleJoker,
    TrippleNormal,
    TrippleJoker,
}