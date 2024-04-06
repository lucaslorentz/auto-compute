namespace LLL.ComputedExpression.AffectedEntitiesProviders;

public class LoadNavigationAffectedEntitiesProvider<TInput, TSourceEntity, TTargetEntity>(
    IAffectedEntitiesProvider<TInput, TSourceEntity> affectedEntitiesProvider,
    IEntityNavigation<TInput, TSourceEntity, TTargetEntity> navigation
) : IAffectedEntitiesProvider<TInput, TTargetEntity>
{
    public string ToDebugString()
    {
        return $"Load({affectedEntitiesProvider.ToDebugString()}, {navigation.Name})";
    }

    public async Task<IReadOnlyCollection<TTargetEntity>> GetAffectedEntitiesAsync(TInput input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);
        return await navigation.LoadCurrentAsync(input, affectedEntities);
    }
}
