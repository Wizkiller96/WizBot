#nullable enable
using System;
using System.Collections.Generic;

namespace NadekoBot.Extensions
{
    public static class LinkedListExtensions
    {
        public static LinkedListNode<T>? FindNode<T>(this LinkedList<T> list, Func<T, bool> predicate)
        {
            var node = list.First;
            while (!(node is null))
            {
                if (predicate(node.Value))
                    return node;
                
                node = node.Next;
            }

            return null;
        }
    }
}