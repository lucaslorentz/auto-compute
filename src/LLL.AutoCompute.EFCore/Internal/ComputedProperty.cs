using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public delegate ComputedProperty ComputedPropertyFactory(IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer, IProperty property);

public abstract class ComputedProperty(IProperty property)
{
    public IProperty Property => property;
    public abstract IEnumerable<ComputedProperty> GetDependencies();
    public abstract Task<int> Update(DbContext dbContext);
}

public class ComputedProperty<TEntity, TValue>(
    IProperty property,
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TValue> changesProvider
) : ComputedProperty(property)
    where TEntity : class
{
    public override IEnumerable<ComputedProperty> GetDependencies()
    {
        return changesProvider.EntityContext.AllAccessedMembers
            .OfType<EFCoreEntityProperty>()
            .Select(e => e.Property.GetComputedProperty())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
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

            var clrType = propertyEntry.Metadata.ClrType;

            var originalValue = entityEntry.State == EntityState.Added
                ? default!
                : (TValue)propertyEntry.OriginalValue!;

            var newValue = changesProvider.ChangeCalculation.ApplyChange(
                originalValue,
                change
            );

            var valueComparer = Property.GetValueComparer();
            if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
            {
                propertyEntry.CurrentValue = newValue;
                numberOfUpdates++;
            }
        }
        return numberOfUpdates;
    }
}
