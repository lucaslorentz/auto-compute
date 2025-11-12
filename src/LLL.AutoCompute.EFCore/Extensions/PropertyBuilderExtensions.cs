using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> AutoCompute<TEntity, TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculatorSelector<TProperty, TProperty>? calculationSelector = null)
        where TEntity : class
    {
        var changeCalculation = calculationSelector?.Invoke(ChangeCalculators<TProperty>.Instance)
            ?? ChangeCalculators<TProperty>.Instance.CurrentValue();

        propertyBuilder.Metadata.SetComputedFactory(
            ComputedMemberFactory.CreateComputedPropertyFactory(
                computedExpression,
                changeCalculation));

        return propertyBuilder;
    }

    public static void ConsistencyCheck<TEntity, TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        Expression<Func<TEntity, bool>> check)
        where TEntity : class
    {
        propertyBuilder.Metadata.SetConsistencyCheck(check);
    }
}
