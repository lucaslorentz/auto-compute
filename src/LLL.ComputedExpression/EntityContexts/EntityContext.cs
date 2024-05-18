using LLL.ComputedExpression.AffectedEntitiesProviders;

namespace LLL.ComputedExpression.EntityContexts;

public abstract class EntityContext
{
    private readonly IList<IEntityMember> _accessedMembers = [];
    private readonly IList<EntityContext> _childContexts = [];

    public IEnumerable<IEntityMember> AccessedMembers => _accessedMembers;
    public IEnumerable<EntityContext> ChildContexts => _childContexts;

    public abstract Type InputType { get; }
    public abstract Type RootEntityType { get; }
    public abstract Type EntityType { get; }
    public abstract bool IsTrackingChanges { get; }

    public void RegisterAccessedMember(IEntityMember member)
    {
        _accessedMembers.Add(member);
    }

    public virtual void RegisterChildContext(EntityContext context)
    {
        _childContexts.Add(context);
    }

    public IAffectedEntitiesProvider? GetAffectedEntitiesProvider()
    {
        var providers = new List<IAffectedEntitiesProvider?>();

        foreach (var member in _accessedMembers)
        {
            if (member is IEntityNavigation entityNavigation)
            {
                var closedType = typeof(NavigationAffectedEntitiesProvider<,,>)
                    .MakeGenericType(entityNavigation.InputType, entityNavigation.SourceEntityType, entityNavigation.TargetEntityType);

                var provider = (IAffectedEntitiesProvider)Activator.CreateInstance(closedType, entityNavigation)!;

                providers.Add(provider);
            }
            else if (member is IEntityProperty entityProperty)
            {
                var closedType = typeof(PropertyAffectedEntitiesProvider<,>)
                    .MakeGenericType(entityProperty.InputType, entityProperty.EntityType);

                var provider = (IAffectedEntitiesProvider)Activator.CreateInstance(closedType, entityProperty)!;

                providers.Add(provider);
            }
        }

        foreach (var childContext in _childContexts)
        {
            var provider = childContext.GetParentAffectedEntitiesProvider();
            if (provider is not null)
                providers.Add(provider);
        }

        return AffectedEntitiesProviderExtensions.ComposeAndCleanup(providers);
    }

    public abstract IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider();

    public virtual void EnrichIncrementalContext(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext)
    {
        foreach (var childContext in _childContexts)
            childContext.EnrichIncrementalContextFromParent(input, entities, incrementalContext);
    }

    public virtual void EnrichIncrementalContextFromParent(object input, IReadOnlyCollection<object> parentEntities, IncrementalContext incrementalContext)
    {
        EnrichIncrementalContext(input, parentEntities, incrementalContext);
    }

    public abstract void EnrichIncrementalContextTowardsRoot(object input, IReadOnlyCollection<object> entities, IncrementalContext incrementalContext);
}
