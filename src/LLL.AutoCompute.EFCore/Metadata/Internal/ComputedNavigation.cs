using System.Collections;
using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedNavigation : ComputedMember
{
}

public class ComputedNavigation<TEntity, TProperty>(
    INavigationBase navigation,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedNavigation, IComputedNavigationBuilder<TEntity, TProperty>
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override INavigationBase Property => navigation;

    public Delegate? ReuseKeySelector { get; set; }
    public IList<IProperty> ReuseUpdateProperties { get; } = [];

    public override string ToDebugString()
    {
        return navigation.ToString()!;
    }

    public override async Task<UpdateChanges> Update(DbContext dbContext)
    {
        var updateChanges = new UpdateChanges();
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = dbContext.Entry(entity);
            var navigationEntry = entityEntry.Navigation(navigation);

            var originalValue = GetOriginalValue(navigationEntry);

            var newValue = ChangesProvider.ChangeCalculation.IsIncremental
                ? ChangesProvider.ChangeCalculation.ApplyChange(
                    originalValue,
                    change)
                : change;

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            MaybeUpdateNavigation(navigationEntry, newValue, updateChanges);
        }
        return updateChanges;
    }

    public override async Task Fix(object entity, DbContext dbContext)
    {
        var entityEntry = dbContext.Entry(entity);
        var navigationEntry = entityEntry.Navigation(navigation);

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            await navigationEntry.LoadAsync();

        var newValue = _compiledExpression((TEntity)entity);

        MaybeUpdateNavigation(navigationEntry, newValue, null);
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }

    private void MaybeUpdateNavigation(
        NavigationEntry navigationEntry,
        TProperty? newValue,
        UpdateChanges? updateChanges)
    {
        if (navigation.IsCollection)
            MaybeUpdateCollection(navigationEntry, newValue, updateChanges);
        else
            MaybeUpdateReference(navigationEntry, newValue, updateChanges);
    }

    private void MaybeUpdateCollection(
        NavigationEntry navigationEntry,
        TProperty? newValue,
        UpdateChanges? updateChanges)
    {
        var dbContext = navigationEntry.EntityEntry.Context;
        var entity = navigationEntry.EntityEntry.Entity;
        var collectionAccessor = navigation.GetCollectionAccessor()!;
        var itemsToRemove = navigationEntry.GetOriginalEntities().ToHashSet();
        foreach (var newItem in (newValue as IEnumerable)!)
        {
            var existingItem = FindEntityToReuse(itemsToRemove, newItem);
            if (existingItem is null)
            {
                collectionAccessor.Add(entity, newItem, false);
                if (updateChanges is not null)
                {
                    updateChanges.AddMemberChange(navigation, entity);
                    if (dbContext.Entry(newItem).State == EntityState.Detached)
                        updateChanges.AddCreatedEntity(navigation.TargetEntityType, newItem);
                }
                continue;
            }

            itemsToRemove.Remove(existingItem);

            foreach (var propertyToUpdate in ReuseUpdateProperties)
            {
                var valueComparer = propertyToUpdate.GetValueComparer();
                var getter = propertyToUpdate.GetGetter();
                var currentEntityEntry = dbContext.Entry(existingItem);
                var currentPropertyEntry = currentEntityEntry.Property(propertyToUpdate);
                var newPropertyValue = getter.GetClrValueUsingContainingEntity(newItem);
                if (!valueComparer.Equals(currentPropertyEntry.CurrentValue, newPropertyValue))
                {
                    currentPropertyEntry.CurrentValue = newPropertyValue;
                    updateChanges?.AddMemberChange(propertyToUpdate, entity);
                }
            }
        }

        foreach (var entityToRemove in itemsToRemove)
        {
            collectionAccessor.Remove(entity, entityToRemove);
            updateChanges?.AddMemberChange(navigation, entity);
        }
    }

    private void MaybeUpdateReference(
        NavigationEntry navigationEntry,
        TProperty? newValue,
        UpdateChanges? updateChanges)
    {
        if (Equals(navigationEntry.CurrentValue, newValue))
            return;

        var dbContext = navigationEntry.EntityEntry.Context;
        var entity = navigationEntry.EntityEntry.Entity;

        navigationEntry.CurrentValue = newValue;

        if (updateChanges is not null)
        {
            updateChanges.AddMemberChange(navigation, entity);
            if (newValue is not null && dbContext.Entry(newValue).State == EntityState.Detached)
                updateChanges.AddCreatedEntity(navigation.TargetEntityType, newValue);
        }
    }

    private object? FindEntityToReuse(IEnumerable<object> availableEntities, object newEntity)
    {
        if (ReuseKeySelector is null)
            return null;

        var reuseKey = ReuseKeySelector.DynamicInvoke(newEntity);
        return availableEntities.FirstOrDefault(x => Equals(ReuseKeySelector.DynamicInvoke(x), reuseKey));
    }
}
