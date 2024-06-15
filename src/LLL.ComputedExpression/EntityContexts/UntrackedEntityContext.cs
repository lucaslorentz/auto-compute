namespace LLL.ComputedExpression.EntityContexts;

public class UntrackedEntityContext : EntityContext
{
    private readonly Type _entityType;
    private readonly EntityContext? _parent;

    public UntrackedEntityContext(Type entityType, EntityContext? parent)
    {
        _entityType = entityType;
        _parent = parent;
        parent?.RegisterChildContext(this);
    }

    public override Type EntityType => _entityType;
    public override bool IsTrackingChanges => false;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return null;
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        if (_parent is not null)
            await _parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }
}
