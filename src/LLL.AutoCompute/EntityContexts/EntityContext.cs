using System.Linq.Expressions;

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

    public abstract IObservedEntityType EntityType { get; }
    public abstract bool IsTrackingChanges { get; }
    public Guid Id { get; } = Guid.NewGuid();
    public Expression Expression => _expression;

    public void RegisterObservedMember(IObservedMember member)
    {
        _observedMembers.Add(member);
    }

    public IEnumerable<IObservedMember> GetAllObservedMembers()
    {
        return GetAllWithDuplicates(this).Distinct();

        IEnumerable<IObservedMember> GetAllWithDuplicates(EntityContext context)
        {
            foreach (var om in context._observedMembers)
                yield return om;

            foreach (var cc in context._childContexts)
            {
                foreach (var om in GetAllWithDuplicates(cc))
                    yield return om;
            }
        }
    }

    public async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(ComputedInput input)
    {
        var entities = new HashSet<object>();

        input.TryGet<IncrementalContext>(out var incrementalContext);

        foreach (var member in _observedMembers)
        {
            if (member is IObservedProperty observedProperty)
            {
                var propertyChanges = await observedProperty.GetChangesAsync(input);
                foreach (var ent in propertyChanges.GetEntityChanges())
                {
                    if (!EntityType.IsInstanceOfType(ent))
                        continue;

                    entities.Add(ent);
                }
            }
            else if (member is IObservedNavigation observedNavigation)
            {
                var navigationChanges = await observedNavigation.GetChangesAsync(input);
                foreach (var (entity, changes) in navigationChanges.GetEntityChanges())
                {
                    if (!EntityType.IsInstanceOfType(entity))
                        continue;

                    entities.Add(entity);
                    if (incrementalContext is not null)
                    {
                        foreach (var addedEntity in changes.Added)
                        {
                            incrementalContext.SetShouldLoadAll(addedEntity);
                            incrementalContext.AddCurrentEntity(entity, observedNavigation, addedEntity);
                        }
                        foreach (var removedEntity in changes.Removed)
                        {
                            incrementalContext.SetShouldLoadAll(removedEntity);
                            incrementalContext.AddOriginalEntity(entity, observedNavigation, removedEntity);
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

    public abstract Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input);

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

    public void ValidateAll()
    {
        ValidateSelf();

        foreach (var childContext in _childContexts)
            childContext.ValidateAll();
    }

    public virtual void ValidateSelf() { }
}
