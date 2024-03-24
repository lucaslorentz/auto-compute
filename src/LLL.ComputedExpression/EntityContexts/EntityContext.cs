using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

public abstract class EntityContext
{
    private readonly IList<IEntityMember> _accessedMembers = [];
    private readonly IList<EntityContext> _childContexts = [];

    public IEnumerable<IEntityMember> AccessedMembers => _accessedMembers;
    public IEnumerable<EntityContext> ChildContexts => _childContexts;

    public abstract bool IsTrackingChanges { get; }

    public void RegisterAccessedMember(IEntityMember member)
    {
        _accessedMembers.Add(member);
    }

    public virtual void RegisterChildContext(EntityContext context)
    {
        _childContexts.Add(context);
    }

    public IAffectedEntitiesProvider GetAffectedEntitiesProvider()
    {
        var compositeProvider = new CompositeAffectedEntitiesProvider();
        foreach (var member in _accessedMembers)
        {
            compositeProvider.AddProvider(member.GetAffectedEntitiesProvider());
        }
        foreach (var childContext in _childContexts)
        {
            var provider = childContext.GetParentAffectedEntitiesProvider();
            if (provider is not null)
                compositeProvider.AddProvider(provider);
        }
        return compositeProvider;
    }

    public abstract IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider();
}
