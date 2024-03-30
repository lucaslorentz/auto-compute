namespace LLL.Computed.EntityContexts;

public class RootEntityContext : EntityContext
{
    public override bool IsTrackingChanges => true;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return null;
    }

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> objects)
    {
        return objects;
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> objects)
    {
        return objects;
    }
}
