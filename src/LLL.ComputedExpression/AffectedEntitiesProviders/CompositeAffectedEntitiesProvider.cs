namespace LLL.ComputedExpression.AffectedEntitiesProviders;

public class CompositeAffectedEntitiesProvider<TInput, TEntity>(
    IReadOnlyCollection<IAffectedEntitiesProvider<TInput, TEntity>> providers
) : IAffectedEntitiesProvider<TInput, TEntity>
{
    private readonly IReadOnlyCollection<IAffectedEntitiesProvider<TInput, TEntity>> _providers = providers;

    public string ToDebugString()
    {
        var inner = string.Join(", ", _providers.Select(p => p.ToDebugString()));

        return $"Concat({inner})";
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext? incrementalContext)
    {
        var entities = new HashSet<TEntity>();
        foreach (var provider in _providers)
        {
            foreach (var entity in await provider.GetAffectedEntitiesAsync(input, incrementalContext))
                entities.Add(entity);
        }
        return entities;
    }

    IReadOnlyCollection<IAffectedEntitiesProvider> IAffectedEntitiesProvider.Flatten()
    {
        return _providers;
    }
}
