namespace LLL.Computed.EntityContexts;

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

    public override IAffectedEntitiesProvider GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider GetAffectedEntitiesProviderInverse()
    {
        return _parent.GetAffectedEntitiesProviderInverse();
    }

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return await _parent.LoadOriginalRootEntities(input, entities);
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return await _parent.LoadCurrentRootEntities(input, entities);
    }
}
