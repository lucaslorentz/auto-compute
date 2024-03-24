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
            var oldValue = collectionAccessor.Create();

            if (!navigationEntry.IsLoaded)
                navigationEntry.Load();

            var inverseNavigation = navigation.Inverse
                ?? throw new Exception("No inverse to compute original vlaue");

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

                    collectionAccessor.AddStandalone(oldValue, item);
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
                            collectionAccessor.AddStandalone(oldValue, itemEntry.Entity);
                    }
                }
            }

            return oldValue;
        }
        else
        {
            var oldKeyValues = navigation.ForeignKey.Properties
                .Select(p => entityEntry.OriginalValues[p])
                .ToArray();

            return entityEntry.Context.Find(navigation.TargetEntityType.ClrType, oldKeyValues);
        }
    }
}