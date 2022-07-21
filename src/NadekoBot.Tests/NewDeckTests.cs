using Nadeko.Econ;
using NUnit.Framework;

namespace NadekoBot.Tests;

public class NewDeckTests
{
    private RegularDeck _deck;

    [SetUp]
    public void Setup()
    {
        _deck = new RegularDeck();
    }

    [Test]
    public void TestCount()
    {
        Assert.AreEqual(52, _deck.TotalCount);
        Assert.AreEqual(52, _deck.CurrentCount);
    }
    
    [Test]
    public void TestDeckDraw()
    {
        var card = _deck.Draw();
        
        Assert.IsNotNull(card);
        Assert.AreEqual(card.Suit, RegularSuit.Hearts);
        Assert.AreEqual(card.Value, RegularValue.A);
        Assert.AreEqual(_deck.CurrentCount, _deck.TotalCount - 1);
    }

    [Test]
    public void TestDeckSpent()
    {
        for (var i = 0; i < _deck.TotalCount - 1; ++i)
        {
            _deck.Draw();
        }

        var lastCard = _deck.Draw();
        
        Assert.IsNotNull(lastCard);
        Assert.AreEqual(lastCard, new RegularCard(RegularSuit.Spades, RegularValue.King));

        var noCard = _deck.Draw();
        
        Assert.IsNull(noCard);
    }
}