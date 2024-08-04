namespace LLL.AutoCompute.EntityContexts;

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

    protected override void NotifyParentsOfAccessedMember(IEntityMember member)
    {
        _parent?.OnAccessedMember(member);
    }
}
