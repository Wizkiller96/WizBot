using System.Collections.Generic;
using NUnit.Framework;

namespace NadekoBot.Tests;

public class ConcurrentHashSetTests
{
    private ConcurrentHashSet<(int?, int?)> _set;

    [SetUp]
    public void SetUp()
    {
        _set = new();
    }

    [Test]
    public void AddTest()
    {
        var result = _set.Add((1, 2));
        
        Assert.AreEqual(true, result);
        
        result = _set.Add((1, 2));
        
        Assert.AreEqual(false, result);
    }

    [Test]
    public void TryRemoveTest()
    {
        _set.Add((1, 2));
        var result = _set.TryRemove((1, 2));
        
        Assert.AreEqual(true, result);
        
        result = _set.TryRemove((1, 2));
        Assert.AreEqual(false, result);
    }

    [Test]
    public void CountTest()
    {
        _set.Add((1, 2)); // 1
        _set.Add((1, 2)); // 1
        
        _set.Add((2, 2)); // 2
        
        _set.Add((3, 2)); // 3
        _set.Add((3, 2)); // 3
        
        Assert.AreEqual(3, _set.Count);
    }

    [Test]
    public void ClearTest()
    {
        _set.Add((1, 2));
        _set.Add((1, 3));
        _set.Add((1, 4));
        
        _set.Clear();
        
        Assert.AreEqual(0, _set.Count);
    }

    [Test]
    public void ContainsTest()
    {
        _set.Add((1, 2));
        _set.Add((3, 2));
        
        Assert.AreEqual(true, _set.Contains((1, 2)));
        Assert.AreEqual(true, _set.Contains((3, 2)));
        Assert.AreEqual(false, _set.Contains((2, 1)));
        Assert.AreEqual(false, _set.Contains((2, 3)));
    }

    [Test]
    public void RemoveWhereTest()
    {
        _set.Add((1, 2));
        _set.Add((1, 3));
        _set.Add((1, 4));
        _set.Add((2, 5));
        
        // remove tuples which have even second item
        _set.RemoveWhere(static x => x.Item2 % 2 == 0);
        
        Assert.AreEqual(2, _set.Count);
        Assert.AreEqual(true, _set.Contains((1, 3)));
        Assert.AreEqual(true, _set.Contains((2, 5)));
    }
}