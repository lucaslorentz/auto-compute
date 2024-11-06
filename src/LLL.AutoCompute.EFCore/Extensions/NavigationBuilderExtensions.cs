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

        navigationBuilder.Metadata.AddComputedFactory(
            ComputedFactory.CreateComputedNavigationFactory(
                computedExpression,
                changeCalculation));

        return navigationBuilder;
    }

    public static NavigationBuilder<TEntity, TProperty> Reaction<TEntity, TProperty, TValue>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        Func<TProperty, TValue, TProperty> mutator)
        where TEntity : class
        where TProperty : class
    {
        return navigationBuilder.Reaction(computedExpression, static c => c.CurrentValue(), mutator);
    }

    public static NavigationBuilder<TEntity, TProperty> AutoComputeKey<TEntity, TProperty, TKey>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TProperty, TKey>> keyExpression,
        Expression<Func<TEntity, TKey>> computedExpression)
        where TEntity : class
        where TProperty : class
    {
        return navigationBuilder.AutoComputeKey(keyExpression, computedExpression, static c => c.CurrentValue());
    }

    public static NavigationBuilder<TEntity, TProperty> AutoComputeKey<TEntity, TProperty, TKey>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TProperty, TKey>> keyExpression,
        Expression<Func<TEntity, TKey>> computedExpression,
        ChangeCalculationSelector<TKey, TKey> calculationSelector)
        where TEntity : class
        where TProperty : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TKey>.Instance);

        navigationBuilder.Metadata.AddComputedFactory(
            ComputedFactory.CreateComputedNavigationKeyFactory(
                computedExpression,
                changeCalculation,
                keyExpression));

        return navigationBuilder;
    }

    public static NavigationBuilder<TEntity, TProperty> Reaction<TEntity, TProperty, TValue, TChange>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<TProperty, TChange, TProperty> applyChange)
        where TEntity : class
        where TProperty : class
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
