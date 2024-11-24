using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedProperty : ComputedMember
{
}

public class ComputedProperty<TEntity, TProperty>(
    IProperty property,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedProperty
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override IProperty Property => property;

    public override async Task<UpdateChanges> Update(DbContext dbContext)
    {
        var updateChanges = new UpdateChanges();
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

            MaybeUpdateProperty(propertyEntry, newValue, updateChanges);
        }
        return updateChanges;
    }

    public override async Task Fix(object entity, DbContext dbContext)
    {
        var entityEntry = dbContext.Entry(entity);
        var propertyEntry = entityEntry.Property(Property);

        var newValue = _compiledExpression((TEntity)entity);

        MaybeUpdateProperty(propertyEntry, newValue, null);
    }

    private static TProperty GetOriginalValue(PropertyEntry propertyEntry)
    {
        if (propertyEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)propertyEntry.OriginalValue!;
    }

    private void MaybeUpdateProperty(PropertyEntry propertyEntry, TProperty? newValue, UpdateChanges? updateChanges)
    {
        var valueComparer = Property.GetValueComparer();
        if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
        {
            propertyEntry.CurrentValue = newValue;
            updateChanges?.AddMemberChange(property, propertyEntry.EntityEntry.Entity);
        }
    }
}
