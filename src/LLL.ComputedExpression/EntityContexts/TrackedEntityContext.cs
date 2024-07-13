namespace LLL.ComputedExpression.EntityContexts;

public class TrackedEntityContext : EntityContext
{
    private readonly Type _entityType;
    private readonly EntityContext? _parent;

    public TrackedEntityContext(Type entityType, EntityContext? parent)
    {
        _entityType = entityType;
        _parent = parent;
        parent?.RegisterChildContext(this);
    }

    public override Type EntityType => _entityType;
    public override bool IsTrackingChanges => true;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        if (_parent is not null)
            await _parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }

    public override void MarkNavigationToLoadAll()
    {
        _parent?.MarkNavigationToLoadAll();
    }
}
