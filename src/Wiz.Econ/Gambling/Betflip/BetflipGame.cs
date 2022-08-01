﻿namespace Wiz.Econ.Gambling;

public sealed class BetflipGame
{
    private readonly decimal _winMulti;
    private static readonly WizBotRandom _rng = new WizBotRandom();

    public BetflipGame(decimal winMulti)
    {
        _winMulti = winMulti;
    }

    public BetflipResult Flip(byte guess, decimal amount)
    {
        var side = (byte)_rng.Next(0, 2);
        if (side == guess)
        {
            return new BetflipResult()
            {
                Side = side,
                Won = amount * _winMulti,
                Multiplier = _winMulti
            };
        }

        return new BetflipResult()
        {
            Side = side,
            Won = 0,
            Multiplier = 0,
        };
    }
}