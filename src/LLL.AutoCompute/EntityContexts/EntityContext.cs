using System.Collections.Concurrent;
using System.Linq.Expressions;
using LLL.AutoCompute.ChangesProviders;

namespace LLL.AutoCompute.EntityContexts;

public abstract class EntityContext
{
    private readonly Expression _expression;
    private readonly IReadOnlyList<EntityContext> _parents;
    private readonly HashSet<IObservedMember> _observedMembers = [];
    private readonly IList<EntityContext> _childContexts = [];

    public EntityContext(Expression expression, IReadOnlyList<EntityContext> parents)
    {
        _expression = expression;
        _parents = parents;

        foreach (var parent in parents)
            parent._childContexts.Add(this);
    }

    public IReadOnlyList<EntityContext> Parents => _parents;
    public IReadOnlySet<IObservedMember> ObservedMembers => _observedMembers;
    public IEnumerable<EntityContext> ChildContexts => _childContexts;
    public abstract string ToDebugString();

    public abstract IObservedEntityType EntityType { get; }
    public abstract bool IsTrackingChanges { get; }
    public Guid Id { get; } = Guid.NewGuid();
    public Expression Expression => _expression;

    public virtual ChangePropagationTarget PropagationTarget => ChangePropagationTarget.AllEntities;

    public virtual bool CanResolveLoadedEntities => true;

    public void RegisterObservedMember(IObservedMember member)
    {
        _observedMembers.Add(member);
    }

    public IEnumerable<IObservedEntityType> GetAllObservedEntityTypes(ChangePropagationTarget? propagationTargetFilter = null)
    {
        return GetAllWithDuplicates(this, pathPropagationTarget: ChangePropagationTarget.AllEntities).Distinct();

        IEnumerable<IObservedEntityType> GetAllWithDuplicates(EntityContext context, ChangePropagationTarget pathPropagationTarget)
        {
            var strictestPropagationTarget = GetStrictestPropagationTarget(pathPropagationTarget, context.PropagationTarget);

            if (propagationTargetFilter is null || strictestPropagationTarget == propagationTargetFilter)
                yield return context.EntityType;

            foreach (var childContext in context._childContexts)
            {
                foreach (var entityType in GetAllWithDuplicates(childContext, strictestPropagationTarget))
                    yield return entityType;
            }
        }
    }

    public IEnumerable<IObservedMember> GetAllObservedMembers(ChangePropagationTarget? propagationTargetFilter = null)
    {
        return GetAllWithDuplicates(this, pathPropagationTarget: ChangePropagationTarget.AllEntities).Distinct();

        IEnumerable<IObservedMember> GetAllWithDuplicates(EntityContext context, ChangePropagationTarget pathPropagationTarget)
        {
            var strictestPropagationTarget = GetStrictestPropagationTarget(pathPropagationTarget, context.PropagationTarget);

            if (propagationTargetFilter is null || strictestPropagationTarget == propagationTargetFilter)
            {
                foreach (var observedMember in context._observedMembers)
                    yield return observedMember;
            }

            foreach (var childContext in context._childContexts)
            {
                foreach (var observedMember in GetAllWithDuplicates(childContext, strictestPropagationTarget))
                    yield return observedMember;
            }
        }

    }

    public async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(ComputedInput input)
    {
        var entities = new HashSet<object>();

        input.TryGet<IncrementalContext>(out var incrementalContext);

        var entityChanges = await EntityType.GetChangesAsync(input);
        if (entityChanges is not null)
        {
            foreach (var entity in entityChanges.Added)
                entities.Add(entity);
            foreach (var entity in entityChanges.Removed)
                entities.Add(entity);
        }

        foreach (var member in _observedMembers)
        {
            if (member is IObservedProperty observedProperty)
            {
                var propertyChanges = await observedProperty.GetChangesAsync(input);
                foreach (var propertyChange in propertyChanges)
                {
                    if (!EntityType.IsInstanceOfType(propertyChange.Entity))
                        continue;

                    entities.Add(propertyChange.Entity);
                }
            }
            else if (member is IObservedNavigation observedNavigation)
            {
                var navigationChanges = await observedNavigation.GetChangesAsync(input);
                foreach (var navigationChange in navigationChanges)
                {
                    if (!EntityType.IsInstanceOfType(navigationChange.Entity))
                        continue;

                    entities.Add(navigationChange.Entity);
                    if (incrementalContext is not null)
                    {
                        foreach (var addedEntity in navigationChange.Added)
                        {
                            incrementalContext.SetShouldLoadAll(addedEntity);
                            incrementalContext.AddCurrentEntity(navigationChange.Entity, observedNavigation, addedEntity);
                        }
                        foreach (var removedEntity in navigationChange.Removed)
                        {
                            incrementalContext.SetShouldLoadAll(removedEntity);
                            incrementalContext.AddOriginalEntity(navigationChange.Entity, observedNavigation, removedEntity);
                        }
                    }
                }
            }
        }

        foreach (var childContext in _childContexts)
        {
            var ents = await childContext.GetParentAffectedEntities(input);
            foreach (var ent in ents)
            {
                if (!EntityType.IsInstanceOfType(ent))
                    continue;

                entities.Add(ent);
            }
        }

        return entities;
    }

    private readonly ConcurrentDictionary<object, EntityContext> _derivedContexts = [];

    public EntityContext DeriveWithCache<T>(T key, Func<T, EntityContext> factory)
        where T : notnull
    {
        return _derivedContexts.GetOrAdd(key, k => factory((T)k));
    }

    public abstract Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input);

    public async Task<IReadOnlyCollection<object>> ResolveLoadedEntitiesAsync(ComputedInput input)
    {
        var entities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var parentLoadedEntities = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var parent in _parents)
        {
            var resolved = await parent.ResolveLoadedEntitiesAsync(input);
            foreach (var entity in resolved)
                parentLoadedEntities.Add(entity);
        }

        if (parentLoadedEntities.Count != 0)
        {
            var loadedEntities = await ResolveLoadedEntitiesFromParentAsync(input, parentLoadedEntities);
            foreach (var entity in loadedEntities)
                entities.Add(entity);
        }

        if (input.TryGet<LoadedEntitySet>(out var loadedEntitySet))
        {
            foreach (var entity in loadedEntitySet.Entities.Where(EntityType.IsInstanceOfType))
                entities.Add(entity);
        }

        return entities;
    }

    protected virtual async Task<IReadOnlyCollection<object>> ResolveLoadedEntitiesFromParentAsync(
        ComputedInput input,
        IReadOnlyCollection<object> parentLoadedEntities)
    {
        return parentLoadedEntities
            .Where(EntityType.IsInstanceOfType)
            .ToArray();
    }

    public virtual async Task EnrichIncrementalContextAsync(ComputedInput input, IReadOnlyCollection<object> entities)
    {
        foreach (var childContext in _childContexts)
            await childContext.EnrichIncrementalContextFromParentAsync(input, entities);
    }

    public virtual async Task EnrichIncrementalContextFromParentAsync(ComputedInput input, IReadOnlyCollection<object> parentEntities)
    {
        await EnrichIncrementalContextAsync(input, parentEntities);
    }

    public virtual async Task EnrichIncrementalContextTowardsRootAsync(ComputedInput input, IReadOnlyCollection<object> entities)
    {
        foreach (var parent in Parents)
            await parent.EnrichIncrementalContextTowardsRootAsync(input, entities);
    }

    public virtual async Task PreLoadNavigationsAsync(ComputedInput input, IReadOnlyCollection<object> entities)
    {
        foreach (var childContext in _childContexts)
            await childContext.PreLoadNavigationsFromParentAsync(input, entities);
    }

    public virtual async Task PreLoadNavigationsFromParentAsync(ComputedInput input, IReadOnlyCollection<object> parentEntities)
    {
        await PreLoadNavigationsAsync(input, parentEntities);
    }

    public virtual void MarkNavigationToLoadAll()
    {
        foreach (var parent in Parents)
            parent.MarkNavigationToLoadAll();
    }

    protected void ValidateAll(EntityContext? firstNotSupportingResolveLoadedEntities)
    {
        ValidateSelf();

        if (firstNotSupportingResolveLoadedEntities is not null
            && PropagationTarget == ChangePropagationTarget.LoadedEntities)
        {
            throw new InvalidOperationException(
                $"EntityContext '{firstNotSupportingResolveLoadedEntities.ToDebugString()}' does not support resolving loaded entities from root and context '{ToDebugString()}' requires loaded entities from root. Remove LoadedEntities propagation target from one of these navigations.");
        }

        var nextNotSupportingResolveLoadedEntities =
            firstNotSupportingResolveLoadedEntities
            ?? (CanResolveLoadedEntities ? null : this);

        foreach (var childContext in _childContexts)
            childContext.ValidateAll(nextNotSupportingResolveLoadedEntities);
    }

    protected virtual void ValidateSelf()
    {
    }

    private static ChangePropagationTarget GetStrictestPropagationTarget(params ChangePropagationTarget[] targets)
    {
        return targets.Any(m => m == ChangePropagationTarget.LoadedEntities)
            ? ChangePropagationTarget.LoadedEntities
            : ChangePropagationTarget.AllEntities;
    }
}
