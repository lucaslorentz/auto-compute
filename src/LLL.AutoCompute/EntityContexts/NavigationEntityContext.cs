using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class NavigationEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly IObservedNavigation _navigation;
    private bool _shouldLoadAll;

    public NavigationEntityContext(
        Expression expression,
        EntityContext parent,
        IObservedNavigation navigation)
        : base(expression)
    {
        _parent = parent;
        _navigation = navigation;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override IObservedEntityType EntityType => _navigation.TargetEntityType;
    public IObservedNavigation Navigation => _navigation;
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        // Short circuit to avoid requiring inverse navigation when no tracked property is accessed
        if (!GetAllObservedMembers().Any())
            return [];

        var inverseNavigation = _navigation.GetInverse();

        var sourceType = _navigation.SourceEntityType;

        var entities = await GetAffectedEntitiesAsync(input, incrementalContext);

        var parentEntities = new HashSet<object>();
        foreach (var (ent, parents) in await inverseNavigation.LoadOriginalAsync(input, entities))
        {
            foreach (var parent in parents)
            {
                if (!sourceType.IsInstanceOfType(parent))
                    continue;

                parentEntities.Add(parent);
                incrementalContext?.AddOriginalEntity(parent, _navigation, ent);
            }
        }
        foreach (var (ent, parents) in await inverseNavigation.LoadCurrentAsync(input, entities))
        {
            foreach (var parent in parents)
            {
                if (!sourceType.IsInstanceOfType(parent))
                    continue;

                parentEntities.Add(parent);
                incrementalContext?.AddCurrentEntity(parent, _navigation, ent);
            }
        }
        return parentEntities;
    }

    public override async Task EnrichIncrementalContextFromParentAsync(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        var entities = new HashSet<object>();

        var parentEntitiesByLoadAll = parentEntities
            .ToLookup(e => _shouldLoadAll || incrementalContext.ShouldLoadAll(e));

        var parentEntitiesToLoadAll = parentEntitiesByLoadAll[true].ToArray();

        foreach (var (parent, ents) in await _navigation.LoadOriginalAsync(input, parentEntitiesToLoadAll))
        {
            foreach (var ent in ents)
            {
                entities.Add(ent);
                incrementalContext.SetShouldLoadAll(ent);
                incrementalContext.AddOriginalEntity(parent, _navigation, ent);
            }
        }

        foreach (var (parent, ents) in await _navigation.LoadCurrentAsync(input, parentEntitiesToLoadAll))
        {
            foreach (var ent in ents)
            {
                entities.Add(ent);
                incrementalContext.SetShouldLoadAll(ent);
                incrementalContext.AddCurrentEntity(parent, _navigation, ent);
            }
        }

        foreach (var parentEntity in parentEntitiesByLoadAll[false])
        {
            foreach (var entity in incrementalContext.GetEntities(parentEntity, _navigation))
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

        foreach (var (ent, parents) in await inverse.LoadOriginalAsync(input, entities))
        {
            foreach (var parent in parents)
            {
                parentEntities.Add(parent);
                incrementalContext.AddOriginalEntity(parent, _navigation, ent);
            }
        }

        foreach (var (ent, parents) in await inverse.LoadCurrentAsync(input, entities))
        {
            foreach (var parent in parents)
            {
                parentEntities.Add(parent);
                incrementalContext.AddCurrentEntity(parent, _navigation, ent);
            }
        }

        await _parent.EnrichIncrementalContextTowardsRootAsync(input, parentEntities, incrementalContext);
    }

    public override async Task PreLoadNavigationsFromParentAsync(object input, IReadOnlyCollection<object> parentEntities)
    {
        var entities = new HashSet<object>();

        foreach (var (parent, ents) in await _navigation.LoadOriginalAsync(input, parentEntities))
        {
            foreach (var ent in ents)
                entities.Add(ent);
        }

        foreach (var (parent, ents) in await _navigation.LoadCurrentAsync(input, parentEntities))
        {
            foreach (var ent in ents)
                entities.Add(ent);
        }

        await PreLoadNavigationsAsync(input, entities);
    }

    public override void MarkNavigationToLoadAll()
    {
        _shouldLoadAll = true;
    }

    public override void ValidateSelf()
    {
        if (GetAllObservedMembers().Any())
        {
            _navigation.GetInverse();
        }
    }
}
