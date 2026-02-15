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
        : base(expression, [parent])
    {
        _parent = parent;
        _navigation = navigation;
        IsTrackingChanges = parent.IsTrackingChanges;
    }

    public override IObservedEntityType EntityType => _navigation.TargetEntityType;
    public IObservedNavigation Navigation => _navigation;
    public override bool IsTrackingChanges { get; }

    public override string ToDebugString() => $"Navigation({_navigation.ToDebugString()})";

    public override ChangePropagationTarget PropagationTarget =>
        _navigation.GetInverse()?.ChangePropagationTarget ?? ChangePropagationTarget.AllEntities;

    public override bool CanResolveLoadedEntities =>
        _navigation.ChangePropagationTarget != ChangePropagationTarget.LoadedEntities;

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input)
    {
        // Short circuit to avoid requiring inverse navigation when no tracked property is accessed
        if (!GetAllObservedMembers().Any())
            return [];

        input.TryGet<IncrementalContext>(out var incrementalContext);

        var sourceType = _navigation.SourceEntityType;

        var entities = await GetAffectedEntitiesAsync(input);

        if (_navigation.ChangePropagationTarget == ChangePropagationTarget.LoadedEntities)
        {
            return await GetParentAffectedLoadedEntities(
                input,
                entities,
                incrementalContext);
        }

        var inverseNavigation = _navigation.GetInverseOrThrow();

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

    private async Task<IReadOnlyCollection<object>> GetParentAffectedLoadedEntities(
        ComputedInput input,
        IReadOnlyCollection<object> affectedEntities,
        IncrementalContext? incrementalContext)
    {
        if (affectedEntities.Count == 0)
            return [];

        var affectedEntitiesSet = affectedEntities.ToHashSet(ReferenceEqualityComparer.Instance);
        var loadedParents = await _parent.ResolveLoadedEntitiesAsync(input);

        if (loadedParents.Count == 0)
            return [];

        var parentEntities = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var (parent, entities) in await _navigation.LoadOriginalAsync(input, loadedParents))
        {
            foreach (var entity in entities)
            {
                if (!affectedEntitiesSet.Contains(entity))
                    continue;

                parentEntities.Add(parent);
                incrementalContext?.AddOriginalEntity(parent, _navigation, entity);
            }
        }

        foreach (var (parent, entities) in await _navigation.LoadCurrentAsync(input, loadedParents))
        {
            foreach (var entity in entities)
            {
                if (!affectedEntitiesSet.Contains(entity))
                    continue;

                parentEntities.Add(parent);
                incrementalContext?.AddCurrentEntity(parent, _navigation, entity);
            }
        }

        return parentEntities;
    }

    protected override async Task<IReadOnlyCollection<object>> ResolveLoadedEntitiesFromParentAsync(
        ComputedInput input,
        IReadOnlyCollection<object> parentLoadedEntities)
    {
        if (parentLoadedEntities.Count == 0)
            return [];

        var entities = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var (_, children) in await _navigation.LoadOriginalAsync(input, parentLoadedEntities))
        {
            foreach (var child in children)
            {
                if (EntityType.IsInstanceOfType(child))
                    entities.Add(child);
            }
        }

        foreach (var (_, children) in await _navigation.LoadCurrentAsync(input, parentLoadedEntities))
        {
            foreach (var child in children)
            {
                if (EntityType.IsInstanceOfType(child))
                    entities.Add(child);
            }
        }

        return entities;
    }

    public override async Task EnrichIncrementalContextFromParentAsync(ComputedInput input, IReadOnlyCollection<object> parentEntities)
    {
        var entities = new HashSet<object>();

        if (!input.TryGet<IncrementalContext>(out var incrementalContext))
            throw new InvalidOperationException("IncrementalContext is required to enrich from parent.");

        var parentEntitiesByLoadAll = parentEntities
            .ToLookup(e => _shouldLoadAll || incrementalContext.ShouldLoadAll(e));

        var parentEntitiesToLoadAll = parentEntitiesByLoadAll[true].ToArray();

        foreach (var (parent, ents) in await _navigation.LoadOriginalAsync(input, parentEntitiesToLoadAll))
        {
            foreach (var ent in ents)
            {
                entities.Add(ent);
                incrementalContext.AddOriginalEntity(parent, _navigation, ent);
                if (incrementalContext.ShouldLoadAll(parent))
                    incrementalContext.SetShouldLoadAll(ent);
            }
        }

        foreach (var (parent, ents) in await _navigation.LoadCurrentAsync(input, parentEntitiesToLoadAll))
        {
            foreach (var ent in ents)
            {
                entities.Add(ent);
                incrementalContext.AddCurrentEntity(parent, _navigation, ent);
                if (incrementalContext.ShouldLoadAll(parent))
                    incrementalContext.SetShouldLoadAll(ent);
            }
        }

        foreach (var parentEntity in parentEntitiesByLoadAll[false])
        {
            foreach (var entity in incrementalContext.GetEntities(parentEntity, _navigation))
            {
                entities.Add(entity);
            }
        }

        await EnrichIncrementalContextAsync(input, entities);
    }

    public override async Task EnrichIncrementalContextTowardsRootAsync(ComputedInput input, IReadOnlyCollection<object> entities)
    {
        if (!input.TryGet<IncrementalContext>(out var incrementalContext))
            throw new InvalidOperationException("IncrementalContext is required to enrich towards root.");

        if (_navigation.ChangePropagationTarget == ChangePropagationTarget.LoadedEntities)
        {
            var loadedParentEntities = await GetParentAffectedLoadedEntities(
                input,
                entities,
                incrementalContext);

            await _parent.EnrichIncrementalContextTowardsRootAsync(input, loadedParentEntities);
            return;
        }

        var inverse = _navigation.GetInverseOrThrow();

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

        await _parent.EnrichIncrementalContextTowardsRootAsync(input, parentEntities);
    }

    public override async Task PreLoadNavigationsFromParentAsync(ComputedInput input, IReadOnlyCollection<object> parentEntities)
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

    protected override void ValidateSelf()
    {
        if (!GetAllObservedMembers().Any())
            return;

        if (PropagationTarget == ChangePropagationTarget.LoadedEntities)
            return;

        _navigation.GetInverseOrThrow();
    }
}
