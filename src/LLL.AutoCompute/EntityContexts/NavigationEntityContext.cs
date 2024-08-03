namespace LLL.AutoCompute.EntityContexts;

public class NavigationEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly IEntityNavigation _navigation;
    private bool _shouldLoadAll;

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

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        var entities = await GetAffectedEntitiesAsync(input, incrementalContext);

        var inverseNavigation = _navigation.GetInverse();

        var parentEntities = new HashSet<object>();
        foreach (var ent in await inverseNavigation.LoadOriginalAsync(input, entities, incrementalContext))
            parentEntities.Add(ent);
        foreach (var ent in await inverseNavigation.LoadCurrentAsync(input, entities, incrementalContext))
            parentEntities.Add(ent);
        return parentEntities;
    }

    public override async Task EnrichIncrementalContextFromParentAsync(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        var entities = new HashSet<object>();

        var parentEntitiesByLoadAll = parentEntities
            .ToLookup(e => _shouldLoadAll || incrementalContext.ShouldLoadAll(e));

        var parentEntitiesToLoadAll = parentEntitiesByLoadAll[true].ToArray();
        var original = await _navigation.LoadOriginalAsync(input, parentEntitiesToLoadAll, incrementalContext);
        var current = await _navigation.LoadCurrentAsync(input, parentEntitiesToLoadAll, incrementalContext);
        foreach (var entity in original.Concat(current))
        {
            incrementalContext.SetShouldLoadAll(entity);
            entities.Add(entity);
        }

        foreach (var parentEntity in parentEntitiesByLoadAll[false])
        {
            foreach (var entity in incrementalContext.GetIncrementalEntities(parentEntity, _navigation))
            {
                entities.Add(entity);
            }
        }

        await EnrichIncrementalContextAsync(input, entities, incrementalContext);
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        var inverse = _navigation.GetInverse();

        var parentEntities = new HashSet<object>();

        foreach (var parent in await inverse.LoadOriginalAsync(input, entities, incrementalContext))
            parentEntities.Add(parent);

        foreach (var parent in await inverse.LoadCurrentAsync(input, entities, incrementalContext))
            parentEntities.Add(parent);

        await _parent.EnrichIncrementalContextTowardsRootAsync(input, parentEntities, incrementalContext);
    }

    public override async Task PreLoadNavigationsFromParentAsync(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        var entities = new HashSet<object>();

        var original = await _navigation.LoadOriginalAsync(input, parentEntities, incrementalContext);
        var current = await _navigation.LoadCurrentAsync(input, parentEntities, incrementalContext);
        foreach (var entity in original.Concat(current))
        {
            entities.Add(entity);
        }

        await PreLoadNavigationsAsync(input, entities, incrementalContext);
    }

    public override void MarkNavigationToLoadAll()
    {
        _shouldLoadAll = true;
    }
}
