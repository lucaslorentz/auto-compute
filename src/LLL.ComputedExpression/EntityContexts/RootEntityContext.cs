using L3.Computed.AffectedEntitiesProviders;

namespace L3.Computed.EntityContexts;

public class RootEntityContext<TInput>
    : IEntityContext
{
    public bool IsTrackingChanges => true;
    public CompositeAffectedEntitiesProvider CompositeAffectedEntitiesProvider { get; } = new();

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
        CompositeAffectedEntitiesProvider.AddProvider(provider);
    }
}
