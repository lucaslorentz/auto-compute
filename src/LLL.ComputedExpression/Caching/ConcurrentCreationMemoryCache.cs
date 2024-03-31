using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace LLL.ComputedExpression.Caching;

public class ConcurrentCreationMemoryCache : IConcurrentCreationCache
{
    private static readonly ConcurrentDictionary<object, object> _locks = new();

    private readonly IMemoryCache _memoryCache;

    public ConcurrentCreationMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T GetOrCreate<K, T>(K key, Func<K, T> create)
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value;
        }

        var creationLock = _locks.GetOrAdd(key!, _ => new object());
        try
        {
            lock (creationLock)
            {
                if (!_memoryCache.TryGetValue(key, out value))
                {
                    value = create(key);
                    _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 10 });
                }

                return value!;
            }
        }
        finally
        {
            _locks.TryRemove(key!, out _);
        }
    }
}
