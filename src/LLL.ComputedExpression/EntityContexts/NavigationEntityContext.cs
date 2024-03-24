using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

public class NavigationEntityContext : EntityContext
{
    private readonly IEntityNavigation _navigation;

    public NavigationEntityContext(
        EntityContext parent,
        IEntityNavigation navigation)
    {
        _navigation = navigation;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider GetParentAffectedEntitiesProvider()
    {
        var inverse = _navigation.GetInverse();
        return new LoadNavigationAffectedEntitiesProvider(GetAffectedEntitiesProvider(), inverse);
    }
}
