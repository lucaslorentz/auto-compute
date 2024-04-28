
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

    public override IReadOnlyCollection<object> GetRequiredIncrementalEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        return base.GetRequiredIncrementalEntities(input, entities, incrementalContext)
            .Concat(GetCascadedAffectedEntities(input, entities, incrementalContext))
            .ToArray();
    }

    public override IReadOnlyCollection<object> GetCascadedAffectedEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        return _parent.GetCascadedAffectedEntities(input, entities, incrementalContext);
    }
}
