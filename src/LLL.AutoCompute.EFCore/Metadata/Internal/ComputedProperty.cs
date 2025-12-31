using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public class ComputedProperty<TEntity, TProperty>(
    IProperty property,
    IComputedChangesProvider<TEntity, TProperty> changesProvider
) : ComputedMember<TEntity, TProperty>(changesProvider)
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public new IComputedChangesProvider<TEntity, TProperty> ChangesProvider => changesProvider;
    public override IProperty Property => property;

    public override async Task<EFCoreChangeset> Update(ComputedInput input)
    {
        var dbContext = input.Get<DbContext>();
        var updateChanges = new EFCoreChangeset();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = dbContext.Entry(entity);
            var propertyEntry = entityEntry.Property(Property);

            var newValue = ChangesProvider.ChangeCalculator.ApplyChange(
                GetOriginalValue(propertyEntry),
                change);

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

    protected override Expression CreateIsValueConsistentExpression(Expression computedValue, Expression storedValue)
    {
        var consistencyCheckEquals = (Expression<Func<TProperty, TProperty, bool>>?)Property.GetConsistencyEquality();

        if (consistencyCheckEquals is not null)
            return consistencyCheckEquals.UnwrapLambda([computedValue, storedValue]);

        return Expression.Call(
            typeof(object), nameof(object.Equals), [],
            Expression.Convert(computedValue, typeof(object)),
            Expression.Convert(storedValue, typeof(object))
        );
    }
}
