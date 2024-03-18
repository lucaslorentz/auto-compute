using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace LLL.Computed;

public class AffectedEntitiesProviderCache : IAffectedEntitiesProviderCache
{
    private static readonly ConcurrentDictionary<object, object> _locks = new();

    private readonly IMemoryCache _memoryCache;

    public AffectedEntitiesProviderCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public IAffectedEntitiesProvider GetOrAdd(
        object cacheKey,
        Func<IAffectedEntitiesProvider> create)
    {
        if (_memoryCache.TryGetValue(cacheKey, out IAffectedEntitiesProvider? affectedEntitiesProvider))
        {
            return affectedEntitiesProvider!;
        }

        var compilationLock = _locks.GetOrAdd(cacheKey, _ => new object());
        try
        {
            lock (compilationLock)
            {
                if (!_memoryCache.TryGetValue(cacheKey, out affectedEntitiesProvider))
                {
                    affectedEntitiesProvider = create();
                    _memoryCache.Set(cacheKey, affectedEntitiesProvider, new MemoryCacheEntryOptions { Size = 10 });
                }

                return affectedEntitiesProvider!;
            }
        }
        finally
        {
            _locks.TryRemove(cacheKey, out _);
        }
    }
}
