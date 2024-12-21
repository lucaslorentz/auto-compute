namespace LLL.AutoCompute.EFCore.Caching;

public interface IConcurrentCreationCache
{
    T GetOrCreate<K, T>(K key, Func<K, T> create)
        where K : notnull;
}
