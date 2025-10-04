using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    private readonly IReadOnlyList<EntityContext> _parents;

    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public IReadOnlyList<EntityContext> Parents => _parents;

    public CompositeEntityContext(
        Expression expression,
        IReadOnlyList<EntityContext> parents)
        : base(expression)
    {
        EntityType = parents[0].EntityType;
        IsTrackingChanges = parents.Any(c => c.IsTrackingChanges);

        foreach (var parent in parents)
            parent.RegisterChildContext(this);

        _parents = parents;
    }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        foreach (var parent in _parents)
            await parent.EnrichIncrementalContextTowardsRootAsync(input, entities, incrementalContext);
    }

    public override void MarkNavigationToLoadAll()
    {
        foreach (var parent in _parents)
            parent.MarkNavigationToLoadAll();
    }
}
