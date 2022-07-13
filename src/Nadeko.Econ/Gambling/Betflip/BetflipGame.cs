namespace Nadeko.Econ.Gambling;

public sealed class BetflipGame
{
    private readonly decimal _winMulti;
    private readonly NadekoRandom _rng;

    public BetflipGame(decimal winMulti)
    {
        _winMulti = winMulti;
        _rng = new NadekoRandom();
    }

    public BetflipResult Flip(byte guess, decimal amount)
    {
        var side = _rng.Next(0, 2);
        decimal won = 0;
        
        if (side == guess)
            won = amount * _winMulti;
        
        return new BetflipResult()
        {
            Side = side, 
            Won = won,
        };
    }
}