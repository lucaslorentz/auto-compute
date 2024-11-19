using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedNavigation : ComputedMember
{
    public abstract INavigationBase Navigation { get; }
}

public class ComputedNavigation<TEntity, TProperty>(
    INavigationBase navigation,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedNavigation
    where TEntity : class
{
    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override INavigationBase Navigation => navigation;

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

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            if (!Equals(navigationEntry.CurrentValue, newValue))
            {
                navigationEntry.CurrentValue = newValue;
                numberOfUpdates++;
            }
        }
        return numberOfUpdates;
    }

    public override async Task Fix(object ent, DbContext dbContext)
    {
        await FixInconsistency((TEntity)ent, dbContext);
    }

    public async Task FixInconsistency(TEntity entity, DbContext dbContext)
    {
        var entityEntry = dbContext.Entry(entity);
        var navigationEntry = entityEntry.Navigation(Navigation);

        var newValue = ((Expression<Func<TEntity, TProperty>>)ChangesProvider.Expression).Compile()(entity);

        if (!Equals(navigationEntry.CurrentValue, newValue))
        {
            navigationEntry.CurrentValue = newValue;
        }
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }
}
