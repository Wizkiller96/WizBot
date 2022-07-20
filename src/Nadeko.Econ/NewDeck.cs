namespace Nadeko.Econ;

public abstract class NewDeck<TCard, TSuit> 
    where TCard: NewCard<TSuit> 
    where TSuit : Enum
{
    public int CurrentCount { get; }
    public int TotalCount { get; }

    public abstract TCard Draw();
}

public abstract class NewCard<TSuit>
    where TSuit: Enum
{
    
}

public sealed class RegularCard : NewCard<RegularSuit>
{
    
}

public enum RegularSuit
{
    
}

public sealed class RegularDeck : NewDeck<RegularCard, RegularSuit>
{
    public override RegularCard Draw()
        => throw new NotImplementedException();
}


