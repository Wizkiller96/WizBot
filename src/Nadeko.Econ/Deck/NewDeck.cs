namespace Nadeko.Econ;

public abstract class NewDeck<TCard, TSuit, TValue> 
    where TCard: NewCard<TSuit, TValue>
    where TSuit : struct, Enum
    where TValue : struct, Enum
{
    public virtual int CurrentCount
        => _cards.Count;
    
    public virtual int TotalCount { get; }

    private readonly LinkedList<TCard> _cards = new();
    public NewDeck()
    {
        var suits = Enum.GetValues<TSuit>();
        var values = Enum.GetValues<TValue>();

        TotalCount =  suits.Length * values.Length;
        
        foreach (var suit in suits)
        {
            foreach (var val in values)
            {
                _cards.AddLast((TCard)Activator.CreateInstance(typeof(TCard), suit, val)!);
            }
        }
    }

    public virtual TCard? Draw()
    {
        var first = _cards.First;
        if (first is not null)
        {
            _cards.RemoveFirst();
            return first.Value;
        }

        return null;
    }

    public virtual TCard? Peek()
        => _cards.First?.Value;

    public virtual void Shuffle()
    {
        var cards = _cards.ToList();
        var newCards = cards.Shuffle();
        
        _cards.Clear();
        foreach (var card in newCards)
            _cards.AddFirst(card);
    }
}