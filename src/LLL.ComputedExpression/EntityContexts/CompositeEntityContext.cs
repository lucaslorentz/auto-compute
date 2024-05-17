namespace LLL.ComputedExpression.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    private readonly IList<EntityContext> _parents;

    public override Type InputType { get; }
    public override Type EntityType { get; }
    public override Type RootEntityType { get; }
    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(IList<EntityContext> parents)
    {
        InputType = parents[0].InputType;
        EntityType = parents[0].EntityType;
        RootEntityType = parents[0].RootEntityType;
        IsTrackingChanges = parents.Any(c => c.IsTrackingChanges);

        foreach (var parent in parents)
            parent.RegisterChildContext(this);

        _parents = parents;
    }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IReadOnlyCollection<object> GetCascadedIncrementalEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        var result = new HashSet<object>();
        foreach (var parent in _parents)
        {
            foreach (var ent in parent.GetCascadedIncrementalEntities(input, entities, incrementalContext))
                result.Add(ent);
        }
        return result;
    }
}
