namespace LLL.ComputedExpression.RootEntitiesProvider;

public class FilteredRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>(
    IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity> parent,
    Func<TInput, TSourceEntity, bool> filter
) : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        var filteredEntities = entities.Where(e => filter(input, e)).ToArray();
        return await parent.GetRootEntitiesAsync(input, filteredEntities);
    }
}
