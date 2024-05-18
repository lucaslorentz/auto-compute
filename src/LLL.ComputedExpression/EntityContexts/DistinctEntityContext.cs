
namespace LLL.ComputedExpression.EntityContexts;

public class DistinctEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public DistinctEntityContext(EntityContext parent)
    {
        _parent = parent;
        InputType = parent.InputType;
        EntityType = parent.EntityType;
        RootEntityType = parent.RootEntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override Type InputType { get; }
    public override Type EntityType { get; }
    public override Type RootEntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override void EnrichIncrementalContext(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        base.EnrichIncrementalContext(input, entities, incrementalContext);
        EnrichIncrementalContextTowardsRoot(input, entities, incrementalContext);
    }

    public override void EnrichIncrementalContextTowardsRoot(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        _parent.EnrichIncrementalContextTowardsRoot(input, entities, incrementalContext);
    }
}
