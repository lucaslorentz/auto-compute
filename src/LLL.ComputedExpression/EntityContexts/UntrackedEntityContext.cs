namespace LLL.Computed.EntityContexts;

public class UntrackedEntityContext : EntityContext
{
    public override bool IsTrackingChanges => false;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }
}
