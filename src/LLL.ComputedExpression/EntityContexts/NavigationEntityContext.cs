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

    public override Type InputType => _parent.InputType;
    public override Type EntityType => _navigation.TargetEntityType;
    public override Type RootEntityType => _parent.RootEntityType;
    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        var affectedEntitiesProvider = GetAffectedEntitiesProvider();

        if (affectedEntitiesProvider is null)
            return null;

        return affectedEntitiesProvider.LoadNavigation(_navigation.GetInverse());
    }

    public override IReadOnlyCollection<object> EnrichIncrementalContextFromParent(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        var entities = _navigation.GetIncrementalEntities(input, parentEntities, incrementalContext);

        var requiredEntities = EnrichIncrementalContext(input, entities, incrementalContext).ToArray();

        var inverse = _navigation.GetInverse() ?? throw new Exception("Inverse not found");

        var originalEntities = inverse.LoadOriginalAsync(input, requiredEntities, incrementalContext).GetAwaiter().GetResult();
        var currentEntities = inverse.LoadCurrentAsync(input, requiredEntities, incrementalContext).GetAwaiter().GetResult();

        return originalEntities.Concat(currentEntities).ToArray();
    }

    public override IReadOnlyCollection<object> GetCascadedIncrementalEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        var inverse = _navigation.GetInverse();

        var parentEntities = inverse.LoadOriginalAsync(input, entities, incrementalContext).GetAwaiter().GetResult()
            .Concat(inverse.LoadCurrentAsync(input, entities, incrementalContext).GetAwaiter().GetResult())
            .ToArray();

        var parentCascaded = _parent.GetCascadedIncrementalEntities(input, parentEntities, incrementalContext).ToArray();

        return _navigation.GetIncrementalEntities(input, parentCascaded, incrementalContext);
    }
}
