using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

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
