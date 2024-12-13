namespace LLL.AutoCompute.EntityContexts;

public class ChangeTrackingEntityContext : EntityContext
{
    private readonly EntityContext? _parent;

    public ChangeTrackingEntityContext(Type entityType, bool trackChanges, EntityContext? parent)
    {
        EntityType = entityType;
        IsTrackingChanges = trackChanges;
        _parent = parent;
        parent?.RegisterChildContext(this);
    }

    public override Type EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
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
