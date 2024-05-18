namespace LLL.ComputedExpression.EntityContexts;

public class UntrackedEntityContext(Type entityType) : EntityContext
{
    public override Type EntityType => entityType;
    public override bool IsTrackingChanges => false;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return null;
    }

    public override void EnrichIncrementalContextTowardsRoot(object input, IReadOnlyCollection<object> entities, IncrementalContext? incrementalContext)
    {
    }
}
