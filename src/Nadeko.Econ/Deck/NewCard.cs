namespace Nadeko.Econ;

public abstract class NewCard<TSuit, TValue>
    where TSuit : struct, Enum
    where TValue : struct, Enum
{
    public TSuit Suit { get; }
    public TValue Value { get; }

    public NewCard(TSuit suit, TValue value)
    {
        Suit = suit;
        Value = value;
    }
}