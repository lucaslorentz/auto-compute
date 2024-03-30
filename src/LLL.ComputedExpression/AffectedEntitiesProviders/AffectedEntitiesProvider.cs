namespace LLL.Computed.AffectedEntitiesProviders;

public static class AffectedEntitiesProvider
{
    public static IAffectedEntitiesProvider? ComposeAndCleanup(IReadOnlyCollection<IAffectedEntitiesProvider?> providers)
    {
        var cleanedUpProviders = providers
            .SelectMany(p => p?.Flatten() ?? [])
            .Where(p => p is not null)
            .DistinctBy(p => p.ToDebugString())
            .ToArray();

        if (cleanedUpProviders.Length == 0)
            return null;

        if (cleanedUpProviders.Length == 1)
            return cleanedUpProviders[0];

        return new CompositeAffectedEntitiesProvider(cleanedUpProviders);
    }
}
