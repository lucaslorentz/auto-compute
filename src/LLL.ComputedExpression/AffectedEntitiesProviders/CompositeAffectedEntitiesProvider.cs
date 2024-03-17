namespace L3.Computed.AffectedEntitiesProviders;

public class CompositeAffectedEntitiesProvider
    : IAffectedEntitiesProvider
{
    private readonly List<IAffectedEntitiesProvider> _providers = [];

    public void AddProvider(IAffectedEntitiesProvider provider)
    {
        _providers.Add(provider);
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
