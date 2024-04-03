namespace LLL.ComputedExpression.RootEntitiesProvider;

public class LoadOriginalNavigationRootEntitiesProvider(
    IRootEntitiesProvider parent,
    IEntityNavigation navigation
) : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var targetEntities = await navigation.LoadOriginalAsync(input, entities);
        return await parent.GetRootEntities(input, targetEntities);
    }
}
