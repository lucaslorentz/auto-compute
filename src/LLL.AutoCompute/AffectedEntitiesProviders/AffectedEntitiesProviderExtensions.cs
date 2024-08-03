using LLL.AutoCompute.Internal;

namespace LLL.AutoCompute.AffectedEntitiesProviders;

public static class AffectedEntitiesProviderExtensions
{
    public static IAffectedEntitiesProvider? LoadNavigation(
        this IAffectedEntitiesProvider affectedEntitiesProvider,
        IEntityNavigation navigation)
    {
        var closedType = typeof(LoadNavigationAffectedEntitiesProvider<,,>).MakeGenericType(affectedEntitiesProvider.InputType, affectedEntitiesProvider.EntityType, navigation.TargetEntityType);
        return (IAffectedEntitiesProvider)Activator.CreateInstance(closedType, affectedEntitiesProvider, navigation)!;
    }

    public static IAffectedEntitiesProvider<TInput, TEntity>? ComposeAndCleanup<TInput, TEntity>(
        IReadOnlyCollection<IAffectedEntitiesProvider<TInput, TEntity>?> providers
    )
    {
        return (IAffectedEntitiesProvider<TInput, TEntity>)ComposeAndCleanup((IReadOnlyCollection<IAffectedEntitiesProvider?>)providers)!;
    }

    public static IAffectedEntitiesProvider? ComposeAndCleanup(
        IReadOnlyCollection<IAffectedEntitiesProvider?> providers
    )
    {
        var cleanedUpProviders = providers
            .SelectMany(p => p?.Flatten() ?? [])
            .DistinctBy(p => p.ToDebugString())
            .ToArray();

        if (cleanedUpProviders.Length == 0)
            return null;

        if (cleanedUpProviders.Length == 1)
            return cleanedUpProviders[0];

        var inputType = cleanedUpProviders.Select(p => p.InputType).Distinct().Single();
        var entityType = cleanedUpProviders.Select(p => p.EntityType).Distinct().Single();

        var providerType = typeof(IAffectedEntitiesProvider<,>).MakeGenericType(inputType, entityType);

        var convertedCleanupUpProviders = cleanedUpProviders.ToArray(providerType);

        var closedType = typeof(CompositeAffectedEntitiesProvider<,>).MakeGenericType(inputType, entityType);
        return (IAffectedEntitiesProvider)Activator.CreateInstance(closedType, convertedCleanupUpProviders)!;
    }
}
