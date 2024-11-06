using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedNavigation(
    INavigationBase navigation,
    IUnboundChangesProvider changesProvider
) : Computed(changesProvider)
{
    public INavigationBase Navigation => navigation;
}

public class ComputedNavigation<TEntity, TProperty>(
    INavigationBase navigation,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedNavigation(navigation, changesProvider)
    where TEntity : class
{
    public new IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;

    public override string ToDebugString()
    {
        return Navigation.ToString()!;
    }

    public override async Task<int> Update(DbContext dbContext)
    {
        var numberOfUpdates = 0;
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = dbContext.Entry(entity);
            var navigationEntry = entityEntry.Navigation(Navigation);

            var newValue = ChangesProvider.ChangeCalculation.IsIncremental
                ? ChangesProvider.ChangeCalculation.ApplyChange(
                    GetOriginalValue(navigationEntry),
                    change)
                : change;

            if (!Equals(navigationEntry.CurrentValue, newValue))
            {
                navigationEntry.CurrentValue = newValue;
                numberOfUpdates++;
            }
        }
        return numberOfUpdates;
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }
}
