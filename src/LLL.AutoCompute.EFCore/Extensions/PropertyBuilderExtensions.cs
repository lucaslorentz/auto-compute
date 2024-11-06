using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> AutoCompute<TEntity, TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
    {
        return propertyBuilder.AutoCompute(computedExpression, static c => c.CurrentValue());
    }

    public static PropertyBuilder<TProperty> AutoCompute<TEntity, TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty> calculationSelector)
        where TEntity : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TProperty>.Instance);

        propertyBuilder.Metadata.AddComputedFactory(
            ComputedFactory.CreateComputedPropertyFactory(
                computedExpression,
                changeCalculation));

        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> Reaction<TEntity, TProperty, TValue>(
        this PropertyBuilder<TProperty> navigationBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        Func<TProperty, TValue, TProperty> applyChange)
        where TEntity : class
    {
        return navigationBuilder.Reaction(computedExpression, static c => c.CurrentValue(), applyChange);
    }

    public static PropertyBuilder<TProperty> Reaction<TEntity, TProperty, TValue, TChange>(
        this PropertyBuilder<TProperty> navigationBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<TProperty, TChange, TProperty> applyChange)
        where TEntity : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TValue>.Instance);

        navigationBuilder.Metadata.AddComputedFactory(
            ComputedFactory.CreateComputedReactionFactory(
                computedExpression,
                changeCalculation,
                applyChange));

        return navigationBuilder;
    }
}
