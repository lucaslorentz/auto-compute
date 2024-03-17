namespace L3.Computed.AffectedEntitiesProviders;

public class LoadNavigationAffectedEntitiesProvider(
    IAffectedEntitiesProvider affectedEntitiesProvider,
    IEntityNavigationLoader loader
) : IAffectedEntitiesProvider
{
    public string ToDebugString()
    {
        var affectedEntities = affectedEntitiesProvider.ToDebugString();
        var navigation = loader.ToDebugString();
        return $"Load({affectedEntities}, {navigation})";
    }

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);
        return await loader.LoadAsync(input, affectedEntities);
    }
}
