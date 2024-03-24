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

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var entities = new HashSet<object>();
        foreach (var provider in providers)
        {
            foreach (var entity in await provider.GetAffectedEntitiesAsync(input))
                entities.Add(entity);
        }
        return entities;
    }
}
