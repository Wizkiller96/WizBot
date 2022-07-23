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
        Assert.AreEqual(card.Value, RegularValue.Ace);
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
        Assert.AreEqual(new RegularCard(RegularSuit.Spades, RegularValue.King), lastCard);

        var noCard = _deck.Draw();
        
        Assert.IsNull(noCard);
    }

    [Test]
    public void TestCardGetName()
    {
        var ace = _deck.Draw()!;
        var two = _deck.Draw()!;
        
        Assert.AreEqual("Ace of Hearts", ace.GetName());
        Assert.AreEqual("Two of Hearts", two.GetName());
    }

    [Test]
    public void TestPeek()
    {
        var ace = _deck.Peek()!;

        var tenOfSpades = _deck.Peek(48);
        Assert.AreEqual(new RegularCard(RegularSuit.Hearts, RegularValue.Ace), ace);
        Assert.AreEqual(new RegularCard(RegularSuit.Spades, RegularValue.Ten), tenOfSpades);
    }

    [Test]
    public void TestMultipleDeck()
    {
        var quadDeck = new MultipleRegularDeck(4);
        var count = quadDeck.TotalCount;
        
        Assert.AreEqual(52 * 4, count);

        var card = quadDeck.Peek(54);
        Assert.AreEqual(new RegularCard(RegularSuit.Hearts, RegularValue.Three), card);
    }
}