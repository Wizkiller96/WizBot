namespace Wiz.Econ;

public sealed record class RegularCard(RegularSuit Suit, RegularValue Value) 
    : NewCard<RegularSuit, RegularValue>(Suit, Value);