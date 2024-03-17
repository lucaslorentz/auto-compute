namespace L3.Computed;

public interface IAffectedEntitiesProviderCache
{
    IAffectedEntitiesProvider GetOrAdd(
        object cacheKey,
        Func<IAffectedEntitiesProvider> create);
}
