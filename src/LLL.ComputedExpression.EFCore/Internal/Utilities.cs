using System.Collections;
using System.Runtime.CompilerServices;
using LLL.ComputedExpression.EFCore.Internal;
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

            foreach (var itemEntry in dbContext.GetComputedInput().ModifiedEntityEntries[navigation.TargetEntityType])
            {
                var inverseReferenceEntry = itemEntry.Reference(inverseNavigation);
                if (inverseReferenceEntry.IsModified)
                {
                    var oldInverseValue = inverseReferenceEntry.GetOriginalValue();
                    if (ReferenceEquals(entityEntry.Entity, oldInverseValue))
                        collectionAccessor.AddStandalone(originalValue, itemEntry.Entity);
                }
            }

            return originalValue;
        }
        else
        {
            if (navigation.ForeignKey.PrincipalEntityType == entityEntry.Metadata)
            {
                var inverseNavigation = navigation.Inverse
                    ?? throw new Exception($"No inverse to compute original value for navigation '{navigation}'");

                var originalValue = navigationEntry.CurrentValue;

                if (entityEntry.State != EntityState.Added)
                {
                    foreach (var itemEntry in dbContext.GetComputedInput().ModifiedEntityEntries[navigation.TargetEntityType])
                    {
                        var inverseReferenceEntry = itemEntry.Reference(inverseNavigation);
                        if (inverseReferenceEntry.IsModified)
                        {
                            var oldInverseValue = inverseReferenceEntry.GetOriginalValue();
                            if (ReferenceEquals(entityEntry.Entity, oldInverseValue))
                                originalValue = itemEntry.Entity;
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

    private readonly static ConditionalWeakTable<IProperty, IEntityProperty> _entityProperties = [];
    public static IEntityProperty GetEntityProperty(this IProperty property)
    {
        return _entityProperties.GetValue(property, static (property) =>
        {
            var closedType = typeof(EFCoreEntityProperty<>).MakeGenericType(property.DeclaringEntityType.ClrType);
            return (IEntityProperty)Activator.CreateInstance(closedType, property)!;
        });
    }

    private readonly static ConditionalWeakTable<INavigation, IEntityNavigation> _entityNavigations = [];
    public static IEntityNavigation GetEntityNavigation(this INavigation navigation)
    {
        return _entityNavigations.GetValue(navigation, static (navigation) =>
        {
            var closedType = typeof(EFCoreEntityNavigation<,>).MakeGenericType(navigation.DeclaringEntityType.ClrType, navigation.TargetEntityType.ClrType);
            return (IEntityNavigation)Activator.CreateInstance(closedType, navigation)!;
        });
    }

    public static async Task BulkLoadAsync<TEntity>(this DbContext dbContext, IEnumerable<TEntity> entities, INavigation navigation)
        where TEntity : class
    {
        var entitiesToLoad = entities.Where(e =>
        {
            var entityEntry = dbContext.Entry(e);
            if (entityEntry.State == EntityState.Detached)
                return false;

            var navigationEntry = entityEntry.Navigation(navigation);
            return !navigationEntry.IsLoaded;
        }).ToArray();

        if (entitiesToLoad.Any())
        {
            await dbContext.Set<TEntity>()
                .Where(e => entitiesToLoad.Contains(e))
                .IgnoreAutoIncludes()
                .Include(e => EF.Property<object>(e, navigation.Name))
                .AsSingleQuery()
                .LoadAsync();
        }
    }
}