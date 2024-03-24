namespace LLL.Computed.AffectedEntitiesProviders;

public class LoadNavigationAffectedEntitiesProvider(
    IAffectedEntitiesProvider affectedEntitiesProvider,
    IEntityNavigation navigation
) : IAffectedEntitiesProvider
{
    public string ToDebugString()
    {
        return $"Load({affectedEntitiesProvider.ToDebugString()}, {navigation.Name})";
    }

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);
        return await navigation.LoadAsync(input, affectedEntities);
    }
}
