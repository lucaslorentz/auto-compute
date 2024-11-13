using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedProperty : ComputedMember
{
    public abstract IProperty Property { get; }
}

public class ComputedProperty<TEntity, TProperty>(
    IProperty property,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedProperty
    where TEntity : class
{
    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override IProperty Property => property;

    public override string ToDebugString()
    {
        return Property.ToDebugString();
    }

    public override async Task<int> Update(DbContext dbContext)
    {
        var numberOfUpdates = 0;
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = dbContext.Entry(entity);
            var propertyEntry = entityEntry.Property(Property);

            var newValue = ChangesProvider.ChangeCalculation.IsIncremental
                ? ChangesProvider.ChangeCalculation.ApplyChange(
                    GetOriginalValue(propertyEntry),
                    change)
                : change;

            var valueComparer = Property.GetValueComparer();
            if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
            {
                propertyEntry.CurrentValue = newValue;
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
        var propertyEntry = entityEntry.Property(Property);

        var newValue = ((Expression<Func<TEntity, TProperty>>)ChangesProvider.Expression).Compile()(entity);

        var valueComparer = Property.GetValueComparer();
        if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
        {
            propertyEntry.CurrentValue = newValue;
        }
    }

    private static TProperty GetOriginalValue(PropertyEntry propertyEntry)
    {
        if (propertyEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)propertyEntry.OriginalValue!;
    }
}
