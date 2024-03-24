namespace LLL.Computed.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(IList<EntityContext> contexts)
    {
        IsTrackingChanges = contexts.Any(c => c.IsTrackingChanges);

        foreach (var context in contexts)
            context.RegisterChildContext(this);
    }

    public override IAffectedEntitiesProvider GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }
}
