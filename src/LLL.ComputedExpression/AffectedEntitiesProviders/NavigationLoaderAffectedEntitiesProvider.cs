namespace L3.Computed.AffectedEntitiesProviders;

public class NavigationLoaderAffectedEntitiesProvider(
    IAffectedEntitiesProvider affectedEntitiesProvider,
    IEntityNavigationLoader loader
) : IAffectedEntitiesProvider
{
    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);
        return await loader.LoadAsync(input, affectedEntities);
    }
}
