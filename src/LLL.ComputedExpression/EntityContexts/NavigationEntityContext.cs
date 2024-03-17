using L3.Computed.AffectedEntitiesProviders;

namespace L3.Computed.EntityContexts;

public class NavigationEntityContext(
    IEntityContext parent,
    IEntityNavigation navigation
) : IEntityContext
{
    public bool IsTrackingChanges => parent.IsTrackingChanges;

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
        if (!IsTrackingChanges) return;

        var inverseLoader = navigation.GetInverseLoader();
        parent.AddAffectedEntitiesProvider(new NavigationLoaderAffectedEntitiesProvider(provider, inverseLoader));
    }
}
