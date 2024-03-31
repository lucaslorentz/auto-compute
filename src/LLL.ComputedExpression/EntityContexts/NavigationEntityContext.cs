using LLL.ComputedExpression.AffectedEntitiesProviders;

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

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var rootEntities = new HashSet<object>();

        var parentEntities = await _navigation.GetInverse().LoadOriginalAsync(input, entities);
        foreach (var rootEntity in await _parent.LoadOriginalRootEntities(input, parentEntities))
            rootEntities.Add(rootEntity);

        return rootEntities;
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var rootEntities = new HashSet<object>();

        var parentEntities = await _navigation.GetInverse().LoadCurrentAsync(input, entities);
        foreach (var rootEntity in await _parent.LoadCurrentRootEntities(input, parentEntities))
            rootEntities.Add(rootEntity);

        return rootEntities;
    }
}
