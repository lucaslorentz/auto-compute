namespace LLL.ComputedExpression.RootEntitiesProviders;

public class LoadOriginalNavigationRootEntitiesProvider<TInput, TRootEntity, TSourceEntity, TTargetEnitty>(
    IRootEntitiesProvider<TInput, TRootEntity, TTargetEnitty> parent,
    IEntityNavigation<TInput, TSourceEntity, TTargetEnitty> navigation
) : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        var targetEntities = await navigation.LoadOriginalAsync(input, entities);
        return await parent.GetRootEntitiesAsync(input, targetEntities);
    }
}
