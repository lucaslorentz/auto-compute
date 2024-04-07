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

    public override Type InputType => _parent.InputType;
    public override Type EntityType => _navigation.TargetEntityType;
    public override Type RootEntityType => _parent.RootEntityType;
    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        var affectedEntitiesProvider = GetAffectedEntitiesProvider();

        if (affectedEntitiesProvider is null)
            return null;

        return affectedEntitiesProvider.LoadNavigation(_navigation.GetInverse());
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        var navigationAffectedEntitiesProvider = _navigation.GetInverse().GetAffectedEntitiesProvider();
        var parentAffectedEntitiesProvider = _parent.GetAffectedEntitiesProviderInverse();

        var loadedFromParentAffectedEntitiesProvider = parentAffectedEntitiesProvider is null
            ? null
            : parentAffectedEntitiesProvider.LoadNavigation(_navigation);

        return AffectedEntitiesProviderExtensions.ComposeAndCleanup([
            navigationAffectedEntitiesProvider,
            loadedFromParentAffectedEntitiesProvider
        ]);
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var closedType = typeof(LoadOriginalNavigationRootEntitiesProvider<,,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType, _parent.EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType, _parent.GetOriginalRootEntitiesProvider(), _navigation.GetInverse())!;
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var closedType = typeof(LoadCurrentNavigationRootEntitiesProvider<,,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType, _parent.EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType, _parent.GetCurrentRootEntitiesProvider(), _navigation.GetInverse())!;
    }
}
