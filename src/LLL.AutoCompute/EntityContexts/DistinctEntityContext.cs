
namespace LLL.AutoCompute.EntityContexts;

public class DistinctEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public DistinctEntityContext(EntityContext parent)
    {
        _parent = parent;
        EntityType = parent.EntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
    }

    public override async Task EnrichIncrementalContextAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        await base.EnrichIncrementalContextAsync(input, entities, incrementalContext);
        await EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        await _parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }

    public override void MarkNavigationToLoadAll()
    {
        _parent.MarkNavigationToLoadAll();
    }
}
