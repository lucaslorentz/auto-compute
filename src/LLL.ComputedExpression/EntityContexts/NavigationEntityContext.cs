using LLL.ComputedExpression.AffectedEntitiesProviders;

namespace LLL.ComputedExpression.EntityContexts;

public class NavigationEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly IEntityNavigation _navigation;

    public NavigationEntityContext(
        EntityContext parent,
        IEntityNavigation navigation)
    {
        _parent = parent;
        _navigation = navigation;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override Type EntityType => _navigation.TargetEntityType;
    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        var affectedEntitiesProvider = GetAffectedEntitiesProvider();

        if (affectedEntitiesProvider is null)
            return null;

        return affectedEntitiesProvider.LoadNavigation(_navigation.GetInverse());
    }

    public override void EnrichIncrementalContextFromParent(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        var entities = parentEntities
            .GroupBy(incrementalContext.ShouldLoadAll)
            .SelectMany(g =>
            {
                if (g.Key)
                {
                    return _navigation.LoadOriginalAsync(input, g.ToArray(), incrementalContext).GetAwaiter().GetResult()
                        .Concat(_navigation.LoadCurrentAsync(input, g.ToArray(), incrementalContext).GetAwaiter().GetResult())
                        .Distinct()
                        .Select(e =>
                        {
                            incrementalContext.SetShouldLoadAll(e);
                            return e;
                        });
                }

                return g
                    .SelectMany(e => incrementalContext.GetIncrementalEntities(e, _navigation))
                    .Distinct();
            })
            .ToHashSet();

        EnrichIncrementalContext(input, entities, incrementalContext);
    }

    public override void EnrichIncrementalContextTowardsRoot(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        var inverse = _navigation.GetInverse();

        var parentEntities = new HashSet<object>();

        foreach (var parent in inverse.LoadOriginalAsync(input, entities, incrementalContext).GetAwaiter().GetResult())
            parentEntities.Add(parent);

        foreach (var parent in inverse.LoadCurrentAsync(input, entities, incrementalContext).GetAwaiter().GetResult())
            parentEntities.Add(parent);

        _parent.EnrichIncrementalContextTowardsRoot(input, parentEntities, incrementalContext);
    }
}
