using System.Collections;
using System.Collections.Concurrent;

namespace AniSort.Server.DataStructures;

public class ConcurrentSet<T> : ISet<T>, IReadOnlySet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, T> dictionary = new();

    private readonly SemaphoreSlim dictLock = new(1, 1);

    /// <inheritdoc />
    bool IReadOnlySet<T>.Contains(T item) => dictionary.ContainsKey(item);

    /// <inheritdoc />
    bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => IsProperSubsetOfInternal(other);

    /// <inheritdoc />
    bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => IsProperSupersetOfInternal(other);

    /// <inheritdoc />
    bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => IsSubsetOfInternal(other);

    /// <inheritdoc />
    bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => IsSupersetOfInternal(other);

    /// <inheritdoc />
    bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => OverlapsInternal(other);

    /// <inheritdoc />
    bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            var enumerable = other.ToList();
            return enumerable.Count == dictionary.Count && enumerable.All(i => dictionary.ContainsKey(i));
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    int IReadOnlyCollection<T>.Count => dictionary.Count;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => dictionary.Keys.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    void ICollection<T>.Add(T item)
    {
        dictLock.Wait();
        try
        {
            dictionary[item] = item;
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    public void ExceptWith(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (var item in other)
            {
                dictionary.TryRemove(item, out _);
            }
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    public void IntersectWith(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            var inBoth = new HashSet<T>();

            foreach (var item in other)
            {
                if (dictionary.ContainsKey(item))
                {
                    inBoth.Add(item);
                }
            }

            foreach (var item in dictionary.Keys.Where(key => !inBoth.Contains(key)))
            {
                dictionary.TryRemove(item, out _);
            }
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) => IsProperSubsetOfInternal(other);

    /// <inheritdoc />
    bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) => IsProperSupersetOfInternal(other);

    /// <inheritdoc />
    bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => IsSubsetOfInternal(other);

    /// <inheritdoc />
    bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => IsSupersetOfInternal(other);

    /// <inheritdoc />
    bool ISet<T>.Overlaps(IEnumerable<T> other) => OverlapsInternal(other);

    /// <inheritdoc />
    bool ISet<T>.SetEquals(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            return other.SequenceEqual(dictionary.Keys);
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            foreach (var item in other)
            {
                if (!dictionary.ContainsKey(item))
                {
                    dictionary.TryAdd(item, item);
                }
                else
                {
                    dictionary.TryRemove(item, out _);
                }
            }
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    public void UnionWith(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            foreach (var item in other)
            {
                dictionary.TryAdd(item, item);
            }
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    bool ISet<T>.Add(T item) => dictionary.TryAdd(item, item);

    /// <inheritdoc />
    public void Clear()
    {
        dictLock.Wait();
        try
        {
            dictionary.Clear();
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    bool ICollection<T>.Contains(T item) => dictionary.ContainsKey(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        dictLock.Wait();
        try
        {
            dictionary.Keys.CopyTo(array, arrayIndex);
        }
        finally
        {
            dictLock.Release();
        }
    }

    /// <inheritdoc />
    public bool Remove(T item) => dictionary.TryRemove(item, out _);

    /// <inheritdoc />
    int ICollection<T>.Count => dictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    private bool OverlapsInternal(IEnumerable<T> other) => other.Any(item => dictionary.ContainsKey(item));

    private bool IsProperSubsetOfInternal(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            int count = other.Count(item => dictionary.ContainsKey(item));

            return count == dictionary.Count;
        }
        finally
        {
            dictLock.Release();
        }
    }

    private bool IsProperSupersetOfInternal(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            var enumerable = other.ToList();

            return !enumerable.Any() || enumerable.All(item => dictionary.ContainsKey(item));
        }
        finally
        {
            dictLock.Release();
        }
    }

    private bool IsSubsetOfInternal(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            int count = other.Count(item => dictionary.ContainsKey(item));

            return count == dictionary.Count && dictionary.Count > 0;
        }
        finally
        {
            dictLock.Wait();
        }
    }

    private bool IsSupersetOfInternal(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            return other.All(item => dictionary.ContainsKey(item));
        }
        finally
        {
            dictLock.Release();
        }
    }

    public int TryRemoveAll(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            return other.Sum(item => dictionary.TryRemove(item, out _) ? 1 : 0);
        }
        finally
        {
            dictLock.Release();
        }
    }

    public int TryAddAll(IEnumerable<T> other)
    {
        dictLock.Wait();
        try
        {
            return other.Sum(item => dictionary.TryAdd(item, item) ? 1 : 0);
        }
        finally
        {
            dictLock.Release();
        }
    }

    public bool TryAdd(T value)
    {
        return dictionary.TryAdd(value, value);
    }

    public bool TryRemove(T value)
    {
        return dictionary.TryRemove(value, out _);
    }
}
