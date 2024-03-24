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
        var providers = new List<IAffectedEntitiesProvider>();
        foreach (var member in _accessedMembers)
        {
            providers.Add(member.GetAffectedEntitiesProvider());
        }
        foreach (var childContext in _childContexts)
        {
            var provider = childContext.GetParentAffectedEntitiesProvider();
            if (provider is not null)
                providers.Add(provider);
        }

        if (providers.Count == 0)
            return new EmptyAffectedEntitiesProvider();

        if (providers.Count == 1)
            return providers[0];

        return new CompositeAffectedEntitiesProvider(providers);
    }

    public abstract IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider();
}
