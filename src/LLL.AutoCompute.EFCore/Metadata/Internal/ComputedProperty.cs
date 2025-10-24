using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public class ComputedProperty<TEntity, TProperty>(
    IProperty property,
    IComputedChangesProvider<EFCoreComputedInput, TEntity, TProperty> changesProvider
) : ComputedMember<TEntity, TProperty>(changesProvider)
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public new IComputedChangesProvider<EFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override IProperty Property => property;

    public override async Task<EFCoreChangeset> Update(EFCoreComputedInput input)
    {
        var updateChanges = new EFCoreChangeset();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = input.DbContext.Entry(entity);
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

    public override async Task FixAsync(object entity, DbContext dbContext)
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

    protected override Expression CreateIsValueInconsistentExpression(Expression computedValue, Expression storedValue)
    {
        return Expression.Not(Expression.Call(
            typeof(object), nameof(object.Equals), [],
            Expression.Convert(computedValue, typeof(object)),
            Expression.Convert(storedValue, typeof(object))
        ));
    }
}
