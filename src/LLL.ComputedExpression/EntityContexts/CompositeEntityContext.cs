namespace L3.Computed.EntityContexts;

public class CompositeEntityContext(
    params IEntityContext[] contexts
) : IEntityContext
{
    public bool IsTrackingChanges => contexts.Any(c => c.IsTrackingChanges);

    public void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider)
    {
        foreach (var context in contexts)
        {
            if (context.IsTrackingChanges)
            {
                context.AddAffectedEntitiesProvider(provider);
            }
        }
    }
}
