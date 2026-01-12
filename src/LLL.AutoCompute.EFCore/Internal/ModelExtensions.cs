using System.Collections;
using System.Reflection;
using LLL.AutoCompute.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public static class ModelExtensions
{
    public static object? GetOriginalValue(this NavigationEntry navigationEntry)
    {
        var entityEntry = navigationEntry.EntityEntry;

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        var dbContext = navigationEntry.EntityEntry.Context;

        var baseNavigation = navigationEntry.Metadata;

        if (navigationEntry is CollectionEntry collectionEntry)
        {
            var collectionAccessor = baseNavigation.GetCollectionAccessor()!;
            var originalValue = collectionAccessor.Create();

            if (baseNavigation is ISkipNavigation skipNavigation)
            {
                // Add current items that are not new
                if (collectionEntry.CurrentValue is not null)
                {
                    foreach (var item in collectionEntry.CurrentValue)
                    {
                        var itemEntry = dbContext.Entry(item);

                        if (skipNavigation.IsRelationshipNew(entityEntry, itemEntry))
                            continue;

                        collectionAccessor.AddStandalone(originalValue, item);
                    }
                }

                // Add items that were in the collection but were removed
                var joinReferenceToSelf = skipNavigation.ForeignKey.DependentToPrincipal;
                var joinReferenceToOther = skipNavigation.Inverse.ForeignKey.DependentToPrincipal;
                if (joinReferenceToSelf is not null && joinReferenceToOther is not null)
                {
                    foreach (var joinEntry in entityEntry.Context.EntityEntriesOfType(skipNavigation.JoinEntityType))
                    {
                        var selfReferenceEntry = joinEntry.Reference(joinReferenceToSelf);
                        var otherReferenceEntry = joinEntry.Reference(joinReferenceToOther!);
                        if ((joinEntry.State == EntityState.Deleted || selfReferenceEntry.IsModified || otherReferenceEntry.IsModified)
                            && joinEntry.State != EntityState.Added
                            && skipNavigation.ForeignKey.IsConnected(entityEntry.OriginalValues, joinEntry.OriginalValues))
                        {
                            collectionAccessor.AddStandalone(originalValue, otherReferenceEntry.GetOriginalValue()!);
                        }
                    }
                }
            }
            else if (baseNavigation is INavigation navigation)
            {
                // Add current items that are not new
                if (collectionEntry.CurrentValue is not null)
                {
                    foreach (var item in collectionEntry.CurrentValue)
                    {
                        var itemEntry = dbContext.Entry(item);

                        if (navigation.IsRelationshipNew(entityEntry, itemEntry))
                            continue;

                        collectionAccessor.AddStandalone(originalValue, item);
                    }
                }

                // Add items that were in the collection but were removed
                foreach (var itemEntry in entityEntry.Context.EntityEntriesOfType(baseNavigation.TargetEntityType))
                {
                    if (!navigation.IsRelated(entityEntry, itemEntry)
                        && navigation.WasRelated(entityEntry, itemEntry))
                    {
                        collectionAccessor.AddStandalone(originalValue, itemEntry.Entity);
                    }
                }
            }

            return originalValue;
        }
        else if (baseNavigation is INavigation navigation)
        {
            var foreignKey = navigation.ForeignKey;
            if (navigation.IsOnDependent)
            {
                var oldKeyValues = foreignKey.Properties
                    .Select(p => entityEntry.OriginalValues[p])
                    .ToArray();

                return entityEntry.Context.Find(
                    baseNavigation.TargetEntityType.ClrType,
                    oldKeyValues);
            }
            else
            {
                if (entityEntry.State != EntityState.Added)
                {
                    var entityOriginalValues = entityEntry.OriginalValues;

                    // Original value is the current value
                    if (navigationEntry is ReferenceEntry referenceEntry
                        && referenceEntry.TargetEntry is not null
                        && referenceEntry.TargetEntry.State != EntityState.Added
                        && foreignKey.IsConnected(entityOriginalValues, referenceEntry.TargetEntry.OriginalValues))
                    {
                        return navigationEntry.CurrentValue;
                    }

                    // Original value was another value
                    var inverseNavigation = baseNavigation.Inverse;
                    if (inverseNavigation is not null)
                    {
                        foreach (var itemEntry in entityEntry.Context.EntityEntriesOfType(baseNavigation.TargetEntityType))
                        {
                            var inverseReferenceEntry = itemEntry.Reference(inverseNavigation);
                            if (inverseReferenceEntry.IsModified
                                && foreignKey.IsConnected(entityOriginalValues, itemEntry.OriginalValues))
                            {
                                return itemEntry.Entity;
                            }
                        }
                    }
                }

                return null;
            }
        }
        else
        {
            throw new NotSupportedException($"Can't get original value of navigation {baseNavigation}");
        }
    }

    public static object? GetCurrentValue(this NavigationEntry navigationEntry)
    {
        var entityEntry = navigationEntry.EntityEntry;

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        var dbContext = navigationEntry.EntityEntry.Context;

        var baseNavigation = navigationEntry.Metadata;

        if (baseNavigation.IsCollection)
        {
            var collectionAccessor = baseNavigation.GetCollectionAccessor()!;
            var currentValue = collectionAccessor.Create();

            if (baseNavigation is ISkipNavigation skipNavigation)
            {
                // Add current items that are still related (not deleted)
                foreach (var item in navigationEntry.GetEntities())
                {
                    var itemEntry = dbContext.Entry(item);

                    if (!skipNavigation.IsRelated(entityEntry, itemEntry))
                        continue;

                    collectionAccessor.AddStandalone(currentValue, item);
                }
            }
            else if (baseNavigation is INavigation navigation)
            {
                // Add current items that are still related (not deleted)
                foreach (var item in navigationEntry.GetEntities())
                {
                    var itemEntry = dbContext.Entry(item);

                    if (!navigation.IsRelated(entityEntry, itemEntry))
                        continue;

                    collectionAccessor.AddStandalone(currentValue, item);
                }
            }

            return currentValue;
        }
        else if (baseNavigation is INavigation navigation && navigationEntry is ReferenceEntry referenceEntry)
        {
            // Ignore current value if it is not related (deleted)
            if (referenceEntry.TargetEntry is not null && !navigation.IsRelated(referenceEntry.EntityEntry, referenceEntry.TargetEntry))
                return null;

            return referenceEntry.CurrentValue;
        }
        else
        {
            throw new NotSupportedException($"Can't get current value of navigation {baseNavigation}");
        }
    }

    public static IReadOnlyCollection<object> GetOriginalEntities(this NavigationEntry navigationEntry)
    {
        var originalValue = navigationEntry.GetOriginalValue();
        if (navigationEntry.Metadata.IsCollection)
        {
            if (originalValue is IEnumerable values)
                return values.OfType<object>().ToArray();
        }
        else if (originalValue is not null)
        {
            return [originalValue];
        }

        return [];
    }

    public static IReadOnlyCollection<object> GetCurrentEntities(this NavigationEntry navigationEntry)
    {
        var currentValue = navigationEntry.GetCurrentValue();
        if (navigationEntry.Metadata.IsCollection)
        {
            if (currentValue is IEnumerable values)
                return values.OfType<object>().ToArray();
        }
        else if (currentValue is not null)
        {
            return [currentValue];
        }

        return [];
    }

    private static IReadOnlyCollection<object> GetEntities(this NavigationEntry navigationEntry)
    {
        var currentValue = navigationEntry.CurrentValue;
        if (navigationEntry.Metadata.IsCollection)
        {
            if (currentValue is IEnumerable values)
                return values.OfType<object>().ToArray();
        }
        else if (currentValue is not null)
        {
            return [currentValue];
        }

        return [];
    }

    public static (IReadOnlyCollection<object> added, IReadOnlyCollection<object> removed) GetModifiedEntities(this NavigationEntry navigationEntry)
    {
        var originalEntities = navigationEntry.EntityEntry.State == EntityState.Added
            ? []
            : navigationEntry.GetOriginalEntities().ToArray();

        var currentEntities = navigationEntry.EntityEntry.State == EntityState.Deleted
            ? []
            : navigationEntry.GetCurrentEntities().ToArray();

        return (
            currentEntities.Except(originalEntities).ToArray(),
            originalEntities.Except(currentEntities).ToArray()
        );
    }

    private static readonly MethodInfo _bulkLoadAsyncTMethodInfo = ((Func<DbContext, IEnumerable<object>, INavigationBase, Task>)BulkLoadAsync<object>)
        .Method.GetGenericMethodDefinition();

    public static async Task BulkLoadAsync(this DbContext dbContext, IEnumerable<object> entities, INavigationBase navigation)
    {
        await (Task)_bulkLoadAsyncTMethodInfo.MakeGenericMethod(navigation.DeclaringEntityType.ClrType)
            .Invoke(null, [dbContext, entities.ToArray(navigation.DeclaringEntityType.ClrType), navigation])!;
    }

    public static async Task BulkLoadAsync<TEntity>(this DbContext dbContext, IEnumerable<TEntity> entities, INavigationBase navigation)
        where TEntity : class
    {
        var entitiesToLoad = entities.Where(e =>
        {
            var entityEntry = dbContext.Entry(e);
            if (entityEntry.State == EntityState.Deleted)
                return false;

            var navigationEntry = entityEntry.Navigation(navigation);
            return !navigationEntry.IsLoaded;
        }).ToArray();

        if (entitiesToLoad.Length != 0)
        {
            await dbContext.Set<TEntity>(navigation.DeclaringEntityType.Name)
                .Where(e => entitiesToLoad.Contains(e))
                .IgnoreAutoIncludes()
                .Include(e => EF.Property<object>(e, navigation.Name))
                .AsSingleQuery()
                .LoadAsync();
        }
    }

    public static bool WasRelated(
        this INavigation navigation,
        EntityEntry entry,
        EntityEntry relatedEntry)
    {
        if (entry.State == EntityState.Added
            || relatedEntry.State == EntityState.Added)
            return false;

        var (principalEntry, dependantEntry) = navigation.IsOnDependent
            ? (relatedEntry, entry)
            : (entry, relatedEntry);

        return navigation.ForeignKey.IsConnected(principalEntry.OriginalValues, dependantEntry.OriginalValues);
    }

    public static bool IsRelated(
        this INavigation navigation,
        EntityEntry entry,
        EntityEntry relatedEntry)
    {
        if (entry.State == EntityState.Deleted
            || relatedEntry.State == EntityState.Deleted)
            return false;

        var (principalEntry, dependantEntry) = navigation.IsOnDependent
            ? (relatedEntry, entry)
            : (entry, relatedEntry);

        return navigation.ForeignKey.IsConnected(principalEntry.CurrentValues, dependantEntry.CurrentValues);
    }

    public static bool WasRelated(
        this ISkipNavigation skipNavigation,
        EntityEntry entry,
        EntityEntry relatedEntry)
    {
        if (entry.State == EntityState.Added
            || relatedEntry.State == EntityState.Added)
            return false;

        var entityValues = entry.CurrentValues;
        var relatedEntityValues = relatedEntry.CurrentValues;
        var foreignKey = skipNavigation.ForeignKey;
        var relatedForeignKey = skipNavigation.Inverse!.ForeignKey;
        foreach (var joinEntry in entry.Context.EntityEntriesOfType(skipNavigation.JoinEntityType))
        {
            if (joinEntry.State == EntityState.Added)
                continue;

            var wasRelated = foreignKey.IsConnected(entityValues, joinEntry.OriginalValues)
                && relatedForeignKey.IsConnected(relatedEntityValues, joinEntry.OriginalValues);

            if (wasRelated)
                return true;
        }

        return false;
    }

    public static bool IsRelated(
        this ISkipNavigation skipNavigation,
        EntityEntry entry,
        EntityEntry relatedEntry)
    {
        if (entry.State == EntityState.Deleted
            || relatedEntry.State == EntityState.Deleted)
            return false;

        var entityValues = entry.CurrentValues;
        var relatedEntityValues = relatedEntry.CurrentValues;
        var foreignKey = skipNavigation.ForeignKey;
        var relatedForeignKey = skipNavigation.Inverse!.ForeignKey;
        foreach (var joinEntry in entry.Context.EntityEntriesOfType(skipNavigation.JoinEntityType))
        {
            if (joinEntry.State == EntityState.Deleted)
                continue;

            var isRelated = foreignKey.IsConnected(entityValues, joinEntry.CurrentValues)
                && relatedForeignKey.IsConnected(relatedEntityValues, joinEntry.CurrentValues);

            if (isRelated)
                return true;
        }

        return false;
    }

    private static bool IsRelationshipNew(
        this INavigation navigation,
        EntityEntry principalEntry,
        EntityEntry dependentEntry
    )
    {
        return !navigation.WasRelated(principalEntry, dependentEntry)
            && navigation.IsRelated(principalEntry, dependentEntry);
    }

    private static bool IsRelationshipNew(
        this ISkipNavigation skipNavigation,
        EntityEntry entry,
        EntityEntry relatedEntry)
    {
        return !skipNavigation.WasRelated(entry, relatedEntry)
            && skipNavigation.IsRelated(entry, relatedEntry);
    }

    private static bool IsConnected(
        this IForeignKey foreignKey,
        PropertyValues principalValues,
        PropertyValues dependentValues)
    {
        for (var i = 0; i < foreignKey.PrincipalKey.Properties.Count; i++)
        {
            var principalProperty = foreignKey.PrincipalKey.Properties[i];
            var dependentProperty = foreignKey.Properties[i];

            var principalValue = principalValues[principalProperty];
            var dependentValue = dependentValues[dependentProperty];

            if (!principalProperty.GetKeyValueComparer().Equals(principalValue, dependentValue))
                return false;
        }

        return true;
    }
}
