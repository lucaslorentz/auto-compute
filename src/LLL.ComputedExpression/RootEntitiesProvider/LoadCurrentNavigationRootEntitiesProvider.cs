﻿namespace LLL.ComputedExpression.RootEntitiesProvider;

public class LoadCurrentNavigationRootEntitiesProvider<TInput, TRootEntity, TSourceEntity, TTargetEnitty>(
    IRootEntitiesProvider<TInput, TRootEntity, TTargetEnitty> parent,
    IEntityNavigation<TInput, TSourceEntity, TTargetEnitty> navigation
) : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntities(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        var targetEntities = await navigation.LoadCurrentAsync(input, entities);
        return await parent.GetRootEntities(input, targetEntities);
    }
}
