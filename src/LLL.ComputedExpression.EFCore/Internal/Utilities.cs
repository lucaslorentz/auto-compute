using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore;

public static class Utilities
{
    public static object? GetOriginalValue(this NavigationEntry navigationEntry)
    {
        var entityEntry = navigationEntry.EntityEntry;

        var dbContext = navigationEntry.EntityEntry.Context;

        var navigation = (navigationEntry.Metadata as INavigation)!;

        if (navigation.IsCollection)
        {
            var collectionAccessor = navigation.GetCollectionAccessor()!;
            var originalValue = collectionAccessor.Create();

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                navigationEntry.Load();

            var inverseNavigation = navigation.Inverse
                ?? throw new Exception($"No inverse to compute original value for navigation '{navigation}'");

            var currentValue = navigationEntry.CurrentValue as IEnumerable;
            if (currentValue is not null)
            {
                foreach (var item in currentValue)
                {
                    var itemEntry = dbContext.Entry(item);
                    if (itemEntry.State == EntityState.Added)
                        continue; // Wasn't in the old list

                    var reference = itemEntry.Reference(inverseNavigation);
                    if (reference.IsModified)
                        // Was added to the list
                        // TODO: Improve to check the foreign keys
                        continue;

                    collectionAccessor.AddStandalone(originalValue, item);
                }
            }

            foreach (var itemEntry in dbContext.ChangeTracker.Entries())
            {
                if (itemEntry.Metadata == navigation.TargetEntityType
                    && itemEntry.State == EntityState.Modified)
                {
                    var inverseReferenceEntry = itemEntry.Reference(inverseNavigation);
                    if (inverseReferenceEntry.IsModified)
                    {
                        var oldInverseValue = inverseReferenceEntry.GetOriginalValue();
                        if (ReferenceEquals(entityEntry.Entity, oldInverseValue))
                            collectionAccessor.AddStandalone(originalValue, itemEntry.Entity);
                    }
                }
            }

            return originalValue;
        }
        else
        {
            var oldKeyValues = navigation.ForeignKey.Properties
                .Select(p => entityEntry.OriginalValues[p])
                .ToArray();

            return entityEntry.Context.Find(navigation.TargetEntityType.ClrType, oldKeyValues);
        }
    }

    public static IEnumerable<object> GetOriginalEntities(this NavigationEntry navigationEntry)
    {
        var originalValue = navigationEntry.GetOriginalValue();
        if (navigationEntry.Metadata.IsCollection)
        {
            if (originalValue is IEnumerable values)
            {
                foreach (var value in values)
                    yield return value;
            }
        }
        else if (originalValue is not null)
        {
            yield return originalValue;
        }
    }

    public static IEnumerable<object> GetEntities(this NavigationEntry navigationEntry)
    {
        var currentValue = navigationEntry.CurrentValue;
        if (navigationEntry.Metadata.IsCollection)
        {
            if (currentValue is IEnumerable values)
            {
                foreach (var value in values)
                    yield return value;
            }
        }
        else if (currentValue is not null)
        {
            yield return currentValue;
        }
    }
}