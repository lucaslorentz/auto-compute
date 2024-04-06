
namespace LLL.ComputedExpression.ChangesProvider;

public class DefaultChangesProvider<TInput, TEntity, TValue>(
    IAffectedEntitiesProvider<TInput, TEntity> affectedEntitiesProvider,
    Func<TInput, TEntity, TValue> originalValueGetter,
    Func<TInput, TEntity, TValue> currentValueGetter,
    IEntityActionProvider<TInput> entityActionProvider
) : IChangesProvider<TInput, TEntity, TValue>
    where TEntity : notnull
{
    public async Task<IReadOnlyDictionary<TEntity, (TValue?, TValue?)>> GetChangesAsync(TInput input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);

        return affectedEntities.ToDictionary(e => e, e =>
        {
            var originalValue = entityActionProvider.GetEntityAction(input, e) == EntityAction.Create
                ? default
                : originalValueGetter(input, e);

            var currentValue = entityActionProvider.GetEntityAction(input, e) == EntityAction.Delete
                ? default
                : currentValueGetter(input, e);

            return (originalValue, currentValue);
        });
    }
}