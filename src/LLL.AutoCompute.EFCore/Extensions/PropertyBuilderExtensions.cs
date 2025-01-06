using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> AutoCompute<TEntity, TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty>? calculationSelector = null)
        where TEntity : class
    {
        var changeCalculation = calculationSelector?.Invoke(ChangeCalculations<TProperty>.Instance)
            ?? ChangeCalculations<TProperty>.Instance.CurrentValue();

        propertyBuilder.Metadata.SetComputedFactory(
            ComputedFactory.CreateComputedPropertyFactory(
                computedExpression,
                changeCalculation));

        return propertyBuilder;
    }
}
