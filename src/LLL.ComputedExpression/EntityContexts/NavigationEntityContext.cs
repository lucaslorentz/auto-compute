using L3.Computed.AffectedEntitiesProviders;

namespace L3.Computed.EntityContexts;

public class NavigationEntityContext : IEntityContext
{
    public bool IsTrackingChanges { get; }
    private readonly CompositeAffectedEntitiesProvider _affectedEntitiesProvider = new();

    public NavigationEntityContext(
        IEntityContext parent,
        IEntityNavigation navigation)
    {
        IsTrackingChanges = parent.IsTrackingChanges;

        if (IsTrackingChanges)
        {
            var inverseLoader = navigation.GetInverseLoader();
            parent.AddAffectedEntitiesProvider(new LoadNavigationAffectedEntitiesProvider(_affectedEntitiesProvider, inverseLoader));
        }
    }

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
        if (!IsTrackingChanges) return;

        _affectedEntitiesProvider.AddProvider(provider);
    }
}
