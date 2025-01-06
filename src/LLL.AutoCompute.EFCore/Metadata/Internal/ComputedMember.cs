using System.Collections;
using System.Collections.Immutable;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedMember(
    IComputedChangesProvider changesProvider)
    : ComputedBase(changesProvider)
{
    public abstract IPropertyBase Property { get; }
    public abstract Task Fix(object entity, DbContext dbContext);

    public override string ToDebugString()
    {
        return $"{Property.DeclaringType.Name}.{Property.Name}";
    }

    protected static void MaybeUpdateProperty(PropertyEntry propertyEntry, object? newValue, EFCoreChangeset? updateChanges)
    {
        var valueComparer = propertyEntry.Metadata.GetValueComparer();
        if (valueComparer.Equals(propertyEntry.CurrentValue, newValue))
            return;

        updateChanges?.AddPropertyChange(propertyEntry.Metadata, propertyEntry.EntityEntry.Entity);

        propertyEntry.CurrentValue = newValue;
    }

    protected static async Task MaybeUpdateNavigation(
        NavigationEntry navigationEntry,
        object? newValue,
        EFCoreChangeset? updateChanges,
        IReadOnlySet<IPropertyBase> updateMembers,
        Delegate? reuseKeySelector)
    {
        var navigation = navigationEntry.Metadata;
        var dbContext = navigationEntry.EntityEntry.Context;
        var entity = navigationEntry.EntityEntry.Entity;
        var itemsToRemove = navigationEntry.GetOriginalEntities().ToHashSet();
        var itemsToAdd = new HashSet<object>();
        var newItems = navigation.IsCollection
            ? (newValue is IEnumerable enumerable ? enumerable : Array.Empty<object>())
            : (newValue is not null ? [newValue] : Array.Empty<object>());

        foreach (var newItem in newItems)
        {
            var existingItem = reuseKeySelector is not null
                ? FindEntityToReuse(itemsToRemove, newItem, reuseKeySelector)
                : null;

            if (existingItem is null)
            {
                if (dbContext.Entry(newItem).State == EntityState.Detached)
                    dbContext.Add(newItem);

                itemsToAdd.Add(newItem);

                if (updateChanges is not null)
                {
                    var entry = dbContext.Entry(newItem);
                    foreach (var member in updateMembers)
                    {
                        var observedMember = member.GetObservedMember();
                        if (observedMember is null)
                            continue;

                        await observedMember.CollectChangesAsync(entry, updateChanges);
                    }
                }
            }
            else
            {
                itemsToRemove.Remove(existingItem);

                foreach (var memberToUpdate in updateMembers)
                {
                    var existingEntityEntry = dbContext.Entry(existingItem);
                    var existingMemberEntry = existingEntityEntry.Member(memberToUpdate);
                    var newMemberValue = memberToUpdate.GetGetter().GetClrValueUsingContainingEntity(newItem);
                    switch (existingMemberEntry)
                    {
                        case PropertyEntry existingPropertyEntry:
                            MaybeUpdateProperty(
                                existingPropertyEntry,
                                newMemberValue,
                                updateChanges);
                            break;
                        case NavigationEntry existingNavigationEntry:
                            await MaybeUpdateNavigation(
                                existingNavigationEntry,
                                newMemberValue,
                                updateChanges,
                                ImmutableHashSet<IPropertyBase>.Empty,
                                null);
                            break;
                        default:
                            throw new NotSupportedException($"Controlled member {memberToUpdate} is not supported");
                    }
                }
            }
        }

        var collectionAccessor = navigation.GetCollectionAccessor();
        foreach (var entityToRemove in itemsToRemove)
        {
            if (collectionAccessor is not null)
                collectionAccessor.Remove(entity, entityToRemove);
            else
                navigationEntry.CurrentValue = null;

            if (updateChanges is not null)
            {
                updateChanges.RegisterNavigationRemoved(navigation, entity, entityToRemove);

                if (navigation.Inverse is not null)
                    updateChanges.RegisterNavigationRemoved(navigation.Inverse, entityToRemove, entity);
            }
        }

        foreach (var entityToAdd in itemsToAdd)
        {
            if (collectionAccessor is not null)
                collectionAccessor.Add(entity, entityToAdd, false);
            else
                navigationEntry.CurrentValue = entityToAdd;

            if (updateChanges is not null)
            {
                updateChanges.RegisterNavigationAdded(navigation, entity, entityToAdd);

                if (navigation.Inverse is not null)
                    updateChanges.RegisterNavigationAdded(navigation.Inverse, entityToAdd, entity);
            }
        }
    }

    private static object? FindEntityToReuse(
        IEnumerable<object> availableEntities,
        object newEntity,
        Delegate reuseKeySelector)
    {
        if (reuseKeySelector is null)
            return null;

        var reuseKey = reuseKeySelector.DynamicInvoke(newEntity);
        return availableEntities.FirstOrDefault(x => Equals(reuseKeySelector.DynamicInvoke(x), reuseKey));
    }
}
