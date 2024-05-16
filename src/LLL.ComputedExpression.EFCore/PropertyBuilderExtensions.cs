using System.Linq.Expressions;
using System.Numerics;
using LLL.ComputedExpression.ChangesProviders;
using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.ComputedExpression.EFCore;

public static class EntityTypeBuilderExtensions
{
    private static PropertyBuilder<TProperty> HasComputedUpdaterFactory<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        ComputedUpdaterFactory updaterFactory)
    {
        return propertyBuilder.HasAnnotation(ComputedAnnotationNames.UpdaterFactory, updaterFactory);
    }

    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);

        propertyBuilder.HasComputedUpdaterFactory((analyzer, property) =>
        {
            try
            {
                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    x => x.CurrentValue())
                    ?? throw new Exception("Computed expression doesn't have change detectors");

                return async (dbContext) =>
                {
                    var changes = 0;
                    var input = dbContext.GetComputedInput();
                    foreach (var (affectedEntity, newValue) in await changesProvider.GetChangesAsync(input, new ChangeMemory<TEntity, TProperty>()))
                    {
                        var affectedEntry = dbContext.Entry(affectedEntity);
                        if (affectedEntry.State == EntityState.Deleted)
                            continue;

                        var propertyEntry = affectedEntry.Property(property);
                        var valueComparer = property.GetValueComparer();
                        if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
                        {
                            propertyEntry.CurrentValue = newValue;
                            changes++;
                        }
                    }
                    return changes;
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}");
            }
        });

        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> IncrementalComputedProperty<TEntity, TValue, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<ChangeCalculationSelector<TValue, TProperty>> calculationSelector,
        Func<TProperty, TProperty, TProperty> updater)
        where TEntity : class
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);
        propertyBuilder.HasComputedUpdaterFactory((analyzer, property) =>
        {
            try
            {
                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    calculationSelector);

                if (changesProvider is null)
                    throw new Exception("Computed expression doesn't have change detectors");

                return async (dbContext) =>
                 {
                     var numberOfUpdates = 0;
                     var input = dbContext.GetComputedInput();
                     var changes = await changesProvider.GetChangesAsync(input, new ChangeMemory<TEntity, TProperty>());
                     foreach (var (entity, value) in changes)
                     {
                         var entityEntry = dbContext.Entry(entity);
                         var propertyEntry = entityEntry.Property(property);

                         var clrType = propertyEntry.Metadata.ClrType;

                         var originalValue = entityEntry.State == EntityState.Added
                             ? default!
                             : (TProperty)propertyEntry.OriginalValue!;

                         var newValue = updater(
                             originalValue,
                             value
                         );

                         var valueComparer = property.GetValueComparer();
                         if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
                         {
                             propertyEntry.CurrentValue = newValue;
                             numberOfUpdates++;
                         }
                     }
                     return numberOfUpdates;
                 };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}", ex);
            }
        });
        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> IncrementalNumberComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
        where TProperty : INumber<TProperty>
    {
        return entityTypeBuilder.IncrementalComputedProperty(
            propertyExpression,
            computedExpression,
            static c => c.NumberIncremental(),
            (c, n) => c + n);
    }
}
