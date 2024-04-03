namespace LLL.ComputedExpression.RootEntitiesProvider;

public class LoadCurrentNavigationRootEntitiesProvider(
    IRootEntitiesProvider parent,
    IEntityNavigation navigation
) : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var targetEntities = await navigation.LoadCurrentAsync(input, entities);
        return await parent.GetRootEntities(input, targetEntities);
    }
}
