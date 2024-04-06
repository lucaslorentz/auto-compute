
namespace LLL.ComputedExpression.EntityContexts;

public class ScopedEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public ScopedEntityContext(EntityContext parent)
    {
        _parent = parent;
        InputType = parent.InputType;
        EntityType = parent.EntityType;
        RootEntityType = parent.RootEntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }


    public override Type InputType { get; }
    public override Type EntityType { get; }
    public override Type RootEntityType {get;}
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
