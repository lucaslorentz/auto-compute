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
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider,
    IReadOnlySet<IPropertyBase> controlledMembers
) : ComputedNavigation, IComputedNavigationBuilder<TEntity, TProperty>
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override INavigationBase Property => navigation;
    public IReadOnlySet<IPropertyBase> ControlledMembers => controlledMembers;
    public Delegate? ReuseKeySelector { get; set; }

    public override string ToDebugString()
    {
        return navigation.ToString()!;
    }

    public override async Task<EFCoreChangeset> Update(IEFCoreComputedInput input)
    {
        var updateChanges = new EFCoreChangeset();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = input.DbContext.Entry(entity);
            var navigationEntry = entityEntry.Navigation(navigation);

            var originalValue = GetOriginalValue(navigationEntry);

            var newValue = ChangesProvider.ChangeCalculation.IsIncremental
                ? ChangesProvider.ChangeCalculation.ApplyChange(
                    originalValue,
                    change)
                : change;

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            await MaybeUpdateNavigation(navigationEntry, newValue, updateChanges, ControlledMembers, ReuseKeySelector);
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

        await MaybeUpdateNavigation(navigationEntry, newValue, null, ControlledMembers, ReuseKeySelector);
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }
}
