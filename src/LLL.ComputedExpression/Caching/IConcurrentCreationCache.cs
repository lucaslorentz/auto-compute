namespace LLL.ComputedExpression.Caching;

public interface IConcurrentCreationCache
{
    T GetOrCreate<K, T>(K key, Func<K, T> create)
        where K : notnull;
}
