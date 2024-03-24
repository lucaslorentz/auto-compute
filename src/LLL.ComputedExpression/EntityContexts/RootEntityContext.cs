namespace LLL.Computed.EntityContexts;

public class RootEntityContext : EntityContext
{
    public override bool IsTrackingChanges => true;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }
}
