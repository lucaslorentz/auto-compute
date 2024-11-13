using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class NavigationBuilderExtensions
{
    public static NavigationBuilder<TEntity, TProperty> AutoCompute<TEntity, TProperty>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
        where TProperty : class
    {
        return navigationBuilder.AutoCompute(computedExpression, static c => c.CurrentValue());
    }

    public static NavigationBuilder<TEntity, TProperty> AutoCompute<TEntity, TProperty>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty> calculationSelector)
        where TEntity : class
        where TProperty : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TProperty>.Instance);

        navigationBuilder.Metadata.SetComputedFactory(
            ComputedFactory.CreateComputedNavigationFactory(
                computedExpression,
                changeCalculation));

        return navigationBuilder;
    }
}
