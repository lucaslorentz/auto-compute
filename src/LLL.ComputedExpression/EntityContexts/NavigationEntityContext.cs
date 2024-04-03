using LLL.ComputedExpression.AffectedEntitiesProviders;
using LLL.ComputedExpression.RootEntitiesProvider;

namespace LLL.ComputedExpression.EntityContexts;

public class NavigationEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly IEntityNavigation _navigation;

    public NavigationEntityContext(
        EntityContext parent,
        IEntityNavigation navigation)
    {
        _parent = parent;
        _navigation = navigation;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        var affectedEntitiesProvider = GetAffectedEntitiesProvider();

        if (affectedEntitiesProvider is null)
            return null;

        return new LoadNavigationAffectedEntitiesProvider(affectedEntitiesProvider, _navigation.GetInverse());
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        var navigationAffectedEntitiesProvider = _navigation.GetInverse().GetAffectedEntitiesProvider();
        var parentAffectedEntitiesProvider = _parent.GetAffectedEntitiesProviderInverse();
        
        var loadedFromParentAffectedEntitiesProvider = parentAffectedEntitiesProvider is null
            ? null
            : new LoadNavigationAffectedEntitiesProvider(parentAffectedEntitiesProvider, _navigation);

        return AffectedEntitiesProvider.ComposeAndCleanup([
            navigationAffectedEntitiesProvider,
            loadedFromParentAffectedEntitiesProvider
        ]);
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        return new LoadOriginalNavigationRootEntitiesProvider(_parent.GetOriginalRootEntitiesProvider(), _navigation.GetInverse());
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        return new LoadCurrentNavigationRootEntitiesProvider(_parent.GetCurrentRootEntitiesProvider(), _navigation.GetInverse());
    }
}
