
namespace LLL.AutoCompute.EntityContexts;

public class ScopedEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public ScopedEntityContext(EntityContext parent)
    {
        _parent = parent;
        EntityType = parent.EntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }


    public override Type EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        await _parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }

    public override void MarkNavigationToLoadAll()
    {
        _parent.MarkNavigationToLoadAll();
    }

    protected override void NotifyParentsOfAccessedMember()
    {
        _parent.OnAccessedMember();
    }
}
