namespace LLL.ComputedExpression.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    private readonly IList<EntityContext> _parents;

    public override Type EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(IList<EntityContext> parents)
    {
        EntityType = parents[0].EntityType;
        IsTrackingChanges = parents.Any(c => c.IsTrackingChanges);

        foreach (var parent in parents)
            parent.RegisterChildContext(this);

        _parents = parents;
    }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        foreach (var parent in _parents)
            await parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }
}
