namespace L3.Computed.EntityContexts;

public class UntrackedEntityContext : IEntityContext
{
    public bool IsTrackingChanges => false;

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
    }
}
