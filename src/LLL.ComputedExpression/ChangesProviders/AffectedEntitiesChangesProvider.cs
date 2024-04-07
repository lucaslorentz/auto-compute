using System.Collections.Immutable;

namespace LLL.ComputedExpression.ChangesProviders;

public abstract class AffectedEntitiesChangesProvider<TInput, TEntity, TValue>(
    IAffectedEntitiesProvider<TInput, TEntity>? affectedEntitiesProvider
) : IChangesProvider<TInput, TEntity, TValue>
    where TEntity : class
{
    public async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync(TInput input)
    {
        if (affectedEntitiesProvider is null)
            return ImmutableDictionary<TEntity, IValueChange<TValue>>.Empty;

        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);

        return new Dictionary<TEntity, IValueChange<TValue>>(
            await Task.WhenAll(
                affectedEntities
                .Select(async entity => new KeyValuePair<TEntity, IValueChange<TValue>>(
                    entity,
                    await GetChangeAsync(input, entity)
                ))
            )
        );
    }

    public abstract Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity);
}