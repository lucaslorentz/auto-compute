using System.Collections.Immutable;

namespace LLL.ComputedExpression.ChangesProviders;

public abstract class AffectedEntitiesChangesProvider<TInput, TEntity, TValue>(
    IAffectedEntitiesProvider<TInput, TEntity>? affectedEntitiesProvider,
    IEqualityComparer<TValue> valueEqualityComparer
) : ChangesProvider<TInput, TEntity, TValue>(valueEqualityComparer)
    where TEntity : class
{
    protected override async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetUnfilteredChangesAsync(TInput input)
    {
        if (affectedEntitiesProvider is null)
            return ImmutableDictionary<TEntity, IValueChange<TValue>>.Empty;

        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);

        var result = new Dictionary<TEntity, IValueChange<TValue>>();

        foreach (var entity in affectedEntities)
            result[entity] = await GetChangeAsync(input, entity);

        return result;
    }
}