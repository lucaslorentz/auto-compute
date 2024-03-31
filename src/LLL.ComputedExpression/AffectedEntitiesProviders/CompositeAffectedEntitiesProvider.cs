namespace LLL.ComputedExpression.AffectedEntitiesProviders;

public class CompositeAffectedEntitiesProvider(
    IReadOnlyCollection<IAffectedEntitiesProvider> providers
) : IAffectedEntitiesProvider
{
    private readonly IReadOnlyCollection<IAffectedEntitiesProvider> _providers = providers;

    public string ToDebugString()
    {
        var inner = string.Join(", ", _providers.Select(p => p.ToDebugString()));

        return $"Concat({inner})";
    }

    public async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input)
    {
        var entities = new HashSet<object>();
        foreach (var provider in _providers)
        {
            foreach (var entity in await provider.GetAffectedEntitiesAsync(input))
                entities.Add(entity);
        }
        return entities;
    }

    IReadOnlyCollection<IAffectedEntitiesProvider> IAffectedEntitiesProvider.Flatten()
    {
        return _providers;
    }
}
