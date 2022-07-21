namespace Nadeko.Econ;

public sealed class RegularCard : NewCard<RegularSuit, RegularValue>
{
    public RegularCard(RegularSuit suit, RegularValue value) : base(suit, value)
    {
    }

    private bool Equals(RegularCard other)
        => other.Suit == this.Suit && other.Value == this.Value;

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is RegularCard other && Equals(other);

    public override int GetHashCode()
        => Suit.GetHashCode() * 17 + Value.GetHashCode();
}