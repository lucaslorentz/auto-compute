namespace LLL.ComputedExpression.Caching;

public interface IConcurrentCreationCache
{
    T GetOrCreate<K, T>(K key, Func<K, T> create)
        where K : notnull;
    Task<T> GetOrCreateAsync<K, T>(K key, Func<K, Task<T>> create)
        where K : notnull;
}
