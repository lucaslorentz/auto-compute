namespace LLL.AutoCompute.EntityContexts;

public class RootEntityContext(Type entityType) : EntityContext
{
    public override Type EntityType => entityType;
    public override bool IsTrackingChanges => true;

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        throw new InvalidOperationException("Can't call GetParentAffectedEntities on RootEntityContext");
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
    }

    public override void MarkNavigationToLoadAll()
    {
    }
}
