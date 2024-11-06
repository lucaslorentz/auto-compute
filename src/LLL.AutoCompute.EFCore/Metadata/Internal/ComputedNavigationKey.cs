using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedNavigationKey(
    INavigationBase navigation,
    IUnboundChangesProvider changesProvider,
    LambdaExpression keyExpression
) : Computed(changesProvider)
{
    public INavigationBase Navigation => navigation;
    public LambdaExpression KeyExpression => keyExpression;
}

public class ComputedNavigationKey<TEntity, TProperty, TKey>(
    INavigationBase navigation,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TKey> changesProvider,
    Expression<Func<TProperty, TKey>> keyExpression
) : ComputedNavigationKey(navigation, changesProvider, keyExpression)
    where TEntity : class
{
    public new IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TKey> ChangesProvider => changesProvider;

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

            var newKey = ChangesProvider.ChangeCalculation.IsIncremental
                ? ChangesProvider.ChangeCalculation.ApplyChange(
                    GetKey(GetOriginalValue(navigationEntry)),
                    change)
                : change;

            if (!Equals(GetKey((TProperty)navigationEntry.CurrentValue!), newKey))
            {
                var newValue = newKey is null
                    ? null
                    : await dbContext.FindAsync(navigationEntry.Metadata.TargetEntityType.ClrType, newKey);

                navigationEntry.CurrentValue = newValue;
                numberOfUpdates++;
            }
        }
        return numberOfUpdates;
    }

    private TKey GetKey(TProperty? value)
    {
        if (value is null)
            return default!;
        return keyExpression.Compile()(value);
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }
}
