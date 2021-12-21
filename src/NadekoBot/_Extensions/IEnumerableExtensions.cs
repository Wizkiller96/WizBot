using System.Security.Cryptography;
using NadekoBot.Common.Collections;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Extensions;

public static class IEnumerableExtensions
{
    public static string JoinWith<T>(this IEnumerable<T> data, char separator, Func<T, string> func = null)
    {
        if (func is null)
            func = x => x?.ToString() ?? string.Empty;

        return string.Join(separator, data.Select(func));
    }
        
    public static string JoinWith<T>(this IEnumerable<T> data, string separator, Func<T, string> func = null)
    {
        func ??= x => x?.ToString() ?? string.Empty;

        return string.Join(separator, data.Select(func));
    }
        
    public static IEnumerable<T> Distinct<T, U>(this IEnumerable<T> data, Func<T, U> getKey) =>
        data.GroupBy(x => getKey(x))
            .Select(x => x.First());

    /// <summary>
    /// Randomize element order by performing the Fisher-Yates shuffle
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    /// <param name="items">Items to shuffle</param>
    public static IReadOnlyList<T> Shuffle<T>(this IEnumerable<T> items)
    {
        using var provider = RandomNumberGenerator.Create();
        var list = items.ToList();
        var n = list.Count;
        while (n > 1)
        {
            var box = new byte[n / Byte.MaxValue + 1];
            int boxSum;
            do
            {
                provider.GetBytes(box);
                boxSum = box.Sum(b => b);
            } while (!(boxSum < n * (Byte.MaxValue * box.Length / n)));

            var k = boxSum % n;
            n--;
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> elems, Action<T> exec)
    {
        var realElems = elems.ToList();
        foreach (var elem in realElems)
        {
            exec(elem);
        }
            
        return realElems;
    }
        
    public static ConcurrentDictionary<TKey, TValue> ToConcurrent<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict)
        => new ConcurrentDictionary<TKey, TValue>(dict);

    public static IndexedCollection<T> ToIndexed<T>(this IEnumerable<T> enumerable)
        where T : class, IIndexed
        => new IndexedCollection<T>(enumerable);

    public static Task<TData[]> WhenAll<TData>(this IEnumerable<Task<TData>> items)
        => Task.WhenAll(items);
}