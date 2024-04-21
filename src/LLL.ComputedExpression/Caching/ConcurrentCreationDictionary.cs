using System.Collections.Concurrent;

namespace LLL.ComputedExpression.Caching;

public class ConcurrentCreationDictionary : IConcurrentCreationCache
{
    private static readonly ConcurrentDictionary<object, SemaphoreSlim> _semaphores = new();

    private readonly ConcurrentDictionary<object, object?> _cache = [];

    public T GetOrCreate<K, T>(K key, Func<K, T> create)
        where K : notnull
    {
        return GetOrCreateAsync(key, (k) => Task.FromResult(create(k))).GetAwaiter().GetResult();
    }

    public async Task<T> GetOrCreateAsync<K, T>(K key, Func<K, Task<T>> create)
        where K : notnull
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return (T)value!;
        }

        var creationLock = _semaphores.GetOrAdd(key!, _ => new SemaphoreSlim(1, 1));
        try
        {
            await creationLock.WaitAsync();

            if (!_cache.TryGetValue(key, out value))
            {
                value = await create(key);
                _cache.TryAdd(key, value);
            }

            return (T)value!;
        }
        finally
        {
            creationLock.Release();
            _semaphores.TryRemove(key!, out _);
        }
    }
}
