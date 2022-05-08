using System;
using System.Collections.Generic;
using AniSort.Core.Models;

namespace AniSort.Core.Extensions;

public static class MergableExtensions
{
    public static IEnumerable<TType> Merge<TType, TKey>(this IEnumerable<TType> first, IEnumerable<TType> second)
        where TType : IMergable<TType, TKey>
        where TKey : IComparable
    {
        var items = new Dictionary<TKey, TType>();

        foreach (var item in second)
        {
            items[item.Key] = item;
        }

        foreach (var item in first)
        {
            if (items.TryGetValue(item.Key, out var other))
            {
                items[item.Key] = item.MergeWith(other);
            }
            else
            {
                items[item.Key] = item;
            }
        }

        return items.Values;
    }
}
