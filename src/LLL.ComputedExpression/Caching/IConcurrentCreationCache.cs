namespace LLL.Computed.Caching;

public interface IConcurrentCreationCache
{
    T GetOrCreate<K, T>(K cacheKey, Func<K, T> create);
}
