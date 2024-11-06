using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedReaction(
    IPropertyBase property,
    IUnboundChangesProvider changesProvider
) : Computed(changesProvider)
{
    public IPropertyBase Property => property;
}
public class ComputedReaction<TEntity, TProperty, TChange>(
    IPropertyBase property,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> changesProvider,
    Func<TProperty, TChange, TProperty> applyChange
) : ComputedReaction(property, changesProvider)
    where TEntity : class
{
    public override string ToDebugString()
    {
        return Property.ToString()!;
    }

    public override async Task<int> Update(DbContext dbContext)
    {
        var numberOfUpdates = 0;
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = dbContext.Entry(entity);
            var memberEntry = entityEntry.Member(Property);

            if (memberEntry is NavigationEntry navigationEntry)
                await navigationEntry.LoadAsync();

            var newValue = applyChange(
                (TProperty)memberEntry.CurrentValue!,
                change);

            if (!Equals(memberEntry.CurrentValue, newValue))
            {
                memberEntry.CurrentValue = newValue;
                numberOfUpdates++;
            }
        }
        return numberOfUpdates;
    }
}
