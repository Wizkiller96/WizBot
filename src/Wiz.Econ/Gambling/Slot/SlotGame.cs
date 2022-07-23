﻿namespace Wiz.Econ.Gambling;

public class SlotGame
{
    private static readonly WizBotRandom _rng = new WizBotRandom();

    public SlotResult Spin(decimal bet)
    {
        var rolls = new[]
        {
            _rng.Next(0, 6),
            _rng.Next(0, 6),
            _rng.Next(0, 6)
        };

        ref var a = ref rolls[0];
        ref var b = ref rolls[1];
        ref var c = ref rolls[2];
        
        var multi = 0;
        var winType = SlotWinType.None;
        if (a == b && b == c)
        {
            if (a == 5)
            {
                winType = SlotWinType.TrippleJoker;
                multi = 30;
            }
            else
            {
                winType = SlotWinType.TrippleNormal;
                multi = 10;
            }
        }
        else if (a == 5 && (b == 5 || c == 5)
                 || (b == 5 && c == 5))
        {
            winType = SlotWinType.DoubleJoker;
            multi = 4;
        }
        else if (a == 5 || b == 5 || c == 5)
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

/*
var rolls = new[]
        {
            _rng.Next(default(byte), 6),
            _rng.Next(default(byte), 6),
            _rng.Next(default(byte), 6)
        };
        
        var multi = 0;
        var winType = SlotWinType.None;

        ref var a = ref rolls[0];
        ref var b = ref rolls[1];
        ref var c = ref rolls[2];
        if (a == b && b == c)
        {
            if (a == 5)
            {
                winType = SlotWinType.TrippleJoker;
                multi = 30;
            }
            else
            {
                winType = SlotWinType.TrippleNormal;
                multi = 10;
            }
        }
        else if (a == 5 && (b == 5 || c == 5)
                 || (b == 5 && c == 5))
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
*/