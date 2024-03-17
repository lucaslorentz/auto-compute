using L3.Computed.AffectedEntitiesProviders;

namespace L3.Computed.EntityContexts;

public class CompositeEntityContext : IEntityContext
{
    public bool IsTrackingChanges { get; }

    private readonly CompositeAffectedEntitiesProvider _affectedEntitiesProvider = new();

    public CompositeEntityContext(params IEntityContext[] contexts)
    {
        IsTrackingChanges = contexts.Any(c => c.IsTrackingChanges);

        foreach (var context in contexts)
        {
            if (context.IsTrackingChanges)
            {
                _affectedEntitiesProvider.AddProvider(_affectedEntitiesProvider);
            }
        }
    }

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
        _affectedEntitiesProvider.AddProvider(provider);
    }
}
