namespace LLL.ComputedExpression.ChangesProviders;

public class LambdaValueChangesProvider<TInput, TEntity, TValue>(
    IAffectedEntitiesProvider<TInput, TEntity>? affectedEntitiesProvider,
    Func<TInput, TEntity, TValue> originalValueGetter,
    Func<TInput, TEntity, TValue> currentValueGetter,
    IEntityActionProvider<TInput> entityActionProvider
) : AffectedEntitiesChangesProvider<TInput, TEntity, TValue>(affectedEntitiesProvider)
    where TEntity : class
{
    public async override Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        return new LazyValueChange<TValue>(
            () =>
            {
                if (entityActionProvider.GetEntityAction(input, entity) == EntityAction.Create)
                    return default!;

                return originalValueGetter(input, entity);
            },
            () =>
            {
                if (entityActionProvider.GetEntityAction(input, entity) == EntityAction.Delete)
                    return default!;

                return currentValueGetter(input, entity);
            }
        );
    }
}