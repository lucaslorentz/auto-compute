namespace LLL.ComputedExpression.EntityContexts;

public class RootEntityContext(Type entityType) : EntityContext
{
    public override Type EntityType => entityType;
    public override bool IsTrackingChanges => true;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override void EnrichIncrementalContextTowardsRoot(object input, IReadOnlyCollection<object> entities, IncrementalContext? incrementalContext)
    {
    }
}
