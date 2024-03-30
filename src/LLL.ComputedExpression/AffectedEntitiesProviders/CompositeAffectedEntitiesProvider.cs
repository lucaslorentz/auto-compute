namespace LLL.Computed.AffectedEntitiesProviders;

public class CompositeAffectedEntitiesProvider(
    IList<IAffectedEntitiesProvider> providers
) : IAffectedEntitiesProvider
{
    public string ToDebugString()
    {
        if (providers is [var provider])
            return provider.ToDebugString();

        var inner = string.Join(", ", providers.Select(p => p.ToDebugString()));

        return $"Concat({inner})";
    }

    public async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input)
    {
        var entities = new HashSet<object>();
        foreach (var provider in providers)
        {
            foreach (var entity in await provider.GetAffectedEntitiesAsync(input))
                entities.Add(entity);
        }
        return entities;
    }

    public static IAffectedEntitiesProvider ComposeIfNecessary(IList<IAffectedEntitiesProvider> providers)
    {
        if (providers.Count == 0)
            return new EmptyAffectedEntitiesProvider();

        if (providers.Count == 1)
            return providers[0];

        return new CompositeAffectedEntitiesProvider(providers);
    }
}
