namespace LLL.AutoCompute.EntityContexts;

public abstract class EntityContext
{
    private readonly HashSet<IObservedMember> _observedMembers = [];
    private readonly IList<EntityContext> _childContexts = [];

    public IReadOnlySet<IObservedMember> ObservedMembers => _observedMembers;
    public IEnumerable<EntityContext> ChildContexts => _childContexts;

    public abstract IObservedEntityType EntityType { get; }
    public abstract bool IsTrackingChanges { get; }

    public void RegisterObservedMember(IObservedMember member)
    {
        _observedMembers.Add(member);
    }

    public IEnumerable<IObservedMember> GetAllObservedMembers()
    {
        foreach (var om in _observedMembers)
            yield return om;

        foreach (var cc in _childContexts)
        {
            foreach (var om in cc.GetAllObservedMembers())
            {
                yield return om;
            }
        }
    }

    public virtual void RegisterChildContext(EntityContext context)
    {
        _childContexts.Add(context);
    }

    public async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext)
    {
        var entities = new HashSet<object>();

        foreach (var member in _observedMembers)
        {
            if (member is IObservedProperty observedProperty)
            {
                var ents = await observedProperty.GetAffectedEntitiesAsync(input);
                foreach (var ent in ents)
                    entities.Add(ent);
            }
            else if (member is IObservedNavigation observedNavigation)
            {
                var navigationChanges = await observedNavigation.GetChangesAsync(input);
                foreach (var (entity, changes) in navigationChanges.GetEntityChanges())
                {
                    entities.Add(entity);
                    foreach (var addedEntity in changes.Added)
                    {
                        incrementalContext?.SetShouldLoadAll(addedEntity);
                        incrementalContext?.AddCurrentEntity(entity, observedNavigation, addedEntity);
                    }
                    foreach (var removedEntity in changes.Removed)
                    {
                        incrementalContext?.SetShouldLoadAll(removedEntity);
                        incrementalContext?.AddOriginalEntity(entity, observedNavigation, removedEntity);
                    }
                }
            }
        }

        foreach (var childContext in _childContexts)
        {
            var ents = await childContext.GetParentAffectedEntities(input, incrementalContext);
            foreach (var ent in ents)
                entities.Add(ent);
        }

        return entities;
    }

    public abstract Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext);

    public virtual async Task EnrichIncrementalContextAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        foreach (var childContext in _childContexts)
            await childContext.EnrichIncrementalContextFromParentAsync(input, entities, incrementalContext);
    }

    public virtual async Task EnrichIncrementalContextFromParentAsync(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        await EnrichIncrementalContextAsync(input, parentEntities, incrementalContext);
    }

    public abstract Task EnrichIncrementalContextTowardsRootAsync(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext);

    public virtual async Task PreLoadNavigationsAsync(object input, IReadOnlyCollection<object> entities)
    {
        foreach (var childContext in _childContexts)
            await childContext.PreLoadNavigationsFromParentAsync(input, entities);
    }

    public virtual async Task PreLoadNavigationsFromParentAsync(object input, IReadOnlyCollection<object> parentEntities)
    {
        await PreLoadNavigationsAsync(input, parentEntities);
    }

    public abstract void MarkNavigationToLoadAll();

    public void ValidateAll()
    {
        ValidateSelf();

        foreach (var childContext in _childContexts)
            childContext.ValidateAll();
    }

    public virtual void ValidateSelf() { }
}
