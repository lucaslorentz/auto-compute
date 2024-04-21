using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace LLL.ComputedExpression.Caching;

public class ConcurrentCreationMemoryCache : IConcurrentCreationCache
{
    private static readonly ConcurrentDictionary<object, SemaphoreSlim> _semaphores = new();

    private readonly IMemoryCache _memoryCache;

    public ConcurrentCreationMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T GetOrCreate<K, T>(K key, Func<K, T> create)
        where K : notnull
    {
        return GetOrCreateAsync(key, (k) => Task.FromResult(create(k))).GetAwaiter().GetResult();
    }

    public async Task<T> GetOrCreateAsync<K, T>(K key, Func<K, Task<T>> create)
        where K : notnull
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value;
        }

        var creationLock = _semaphores.GetOrAdd(key!, _ => new SemaphoreSlim(1, 1));
        try
        {
            await creationLock.WaitAsync();

            if (!_memoryCache.TryGetValue(key, out value))
            {
                value = await create(key);
                _memoryCache.Set(key, value, new MemoryCacheEntryOptions { Size = 10 });
            }

            return value!;
        }
        finally
        {
            creationLock.Release();
            _semaphores.TryRemove(key!, out _);
        }
    }
}
