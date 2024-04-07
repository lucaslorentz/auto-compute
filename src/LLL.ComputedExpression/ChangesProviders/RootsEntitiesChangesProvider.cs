namespace LLL.ComputedExpression.ChangesProviders;

public class RootsChangesProvider<TInput, TEntity, TRootEntity>(
    IAffectedEntitiesProvider<TInput, TEntity> affectedEntitiesProvider,
    IRootEntitiesProvider<TInput, TRootEntity, TEntity> originalRootsProvider,
    IRootEntitiesProvider<TInput, TRootEntity, TEntity> currentRootsProvider,
    IEntityActionProvider<TInput> entityActionProvider
) : AffectedEntitiesChangesProvider<TInput, TEntity, IReadOnlyCollection<TRootEntity>>(affectedEntitiesProvider)
    where TEntity : class
{
    public async override Task<IValueChange<IReadOnlyCollection<TRootEntity>>> GetChangeAsync(TInput input, TEntity entity)
    {
        var entityAction = entityActionProvider.GetEntityAction(input, entity);

        var originalRoots = entityAction == EntityAction.Create
            ? []
            : await originalRootsProvider.GetRootEntitiesAsync(input, [entity]);

        var currentRoots = entityAction == EntityAction.Delete
            ? []
            : await currentRootsProvider.GetRootEntitiesAsync(input, [entity]);

        return new ConstValueChange<IReadOnlyCollection<TRootEntity>>(
            originalRoots,
            currentRoots
        );
    }
}