using System.Linq.Expressions;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class EntityTypeBuilderExtensions
{
    private static PropertyBuilder<TProperty> HasComputedPropertyFactory<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        ComputedPropertyFactory computedPropertyFactory)
    {
        return propertyBuilder.HasAnnotation(ComputedAnnotationNames.PropertyFactory, computedPropertyFactory);
    }

    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
    {
        return ComputedProperty(
            entityTypeBuilder,
            propertyExpression,
            computedExpression,
            static c => c.CurrentValue());
    }

    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TValue, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<ChangeCalculationSelector<TValue, TProperty>> calculationSelector)
        where TEntity : class
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);
        propertyBuilder.HasComputedPropertyFactory((analyzer, property) =>
        {
            try
            {
                var calculation = calculationSelector.Compile()(new ChangeCalculations<TValue>());

                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    calculationSelector);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedProperty<TEntity, TProperty>(property, changesProvider);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}", ex);
            }
        });
        return propertyBuilder;
    }
}
