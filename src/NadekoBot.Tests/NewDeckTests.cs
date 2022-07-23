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
        Assert.AreEqual(lastCard, new RegularCard(RegularSuit.Spades, RegularValue.King));

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
        Assert.AreEqual(ace, new RegularCard(RegularSuit.Hearts, RegularValue.Ace));
        Assert.AreEqual(tenOfSpades, new RegularCard(RegularSuit.Spades, RegularValue.Ten));
    }
}