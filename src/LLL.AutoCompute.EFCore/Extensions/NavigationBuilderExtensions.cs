using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class NavigationBuilderExtensions
{
    public static NavigationBuilder<TEntity, TProperty> AutoCompute<TEntity, TProperty>(
        this NavigationBuilder<TEntity, TProperty> navigationBuilder,
        Expression<Func<TEntity, TProperty?>> computedExpression,
        ChangeCalculatorSelector<TProperty, TProperty>? calculationSelector = null,
        Action<IComputedNavigationBuilder<TEntity, TProperty>>? configure = null)
        where TEntity : class
        where TProperty : class
    {
        var changeCalculator = calculationSelector?.Invoke(ChangeCalculatorFactory<TProperty>.Instance)
            ?? ChangeCalculatorFactory<TProperty>.Instance.CurrentValue();

        navigationBuilder.Metadata.SetComputedFactory(
            ComputedMemberFactory.CreateComputedNavigationFactory(
                computedExpression!,
                changeCalculator,
                configure));

        return navigationBuilder;
    }
}
