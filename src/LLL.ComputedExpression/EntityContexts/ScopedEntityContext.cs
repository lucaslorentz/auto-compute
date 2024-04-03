namespace LLL.ComputedExpression.EntityContexts;

public class ScopedEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public ScopedEntityContext(EntityContext parent)
    {
        _parent = parent;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return _parent.GetAffectedEntitiesProviderInverse();
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        return _parent.GetOriginalRootEntitiesProvider();
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        return _parent.GetCurrentRootEntitiesProvider();
    }
}
