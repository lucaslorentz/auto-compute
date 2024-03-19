namespace LLL.Computed.AffectedEntitiesProviders;

public class LoadNavigationAffectedEntitiesProvider(
    IAffectedEntitiesProvider affectedEntitiesProvider,
    IEntityNavigation navigation
) : IAffectedEntitiesProvider
{
    public string ToDebugString()
    {
        var affectedEntities = affectedEntitiesProvider.ToDebugString();
        var navigationDebugString = navigation.ToDebugString();
        return $"Load({affectedEntities}, {navigationDebugString})";
    }

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);
        return await navigation.LoadAsync(input, affectedEntities);
    }
}
