namespace L3.Computed.AffectedEntitiesProviders;

public class CompositeAffectedEntitiesProvider
    : IAffectedEntitiesProvider
{
    private readonly List<IAffectedEntitiesProvider> _providers = [];

    public void AddProvider(IAffectedEntitiesProvider provider)
    {
        _providers.Add(provider);
    }

    public string ToDebugString()
    {
        if (_providers is [var provider])
            return provider.ToDebugString();

        var inner = string.Join(", ", _providers.Select(p => p.ToDebugString()));

        return $"Concat({inner})";
    }

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var entities = new HashSet<object>();
        foreach (var provider in _providers)
        {
            foreach (var entity in await provider.GetAffectedEntitiesAsync(input))
                entities.Add(entity);
        }
        return entities;
    }
}
