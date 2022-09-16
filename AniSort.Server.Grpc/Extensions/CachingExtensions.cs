using System.Runtime.Caching;

namespace AniSort.Server.Extensions;

public static class CachingExtensions
{
    public static T GetOrFetch<T>(this MemoryCache cache, string key, CacheItemPolicy cacheItemPolicy, Func<string, T> fetch) where T : class
    {
#pragma warning disable CS8604
        var entry = cache.Get(key);
#pragma warning restore CS8604

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (entry == null)
        {
            entry = fetch(key);
            cache.Add(key, entry, cacheItemPolicy);
        }

        return (T) entry;
    }
}
