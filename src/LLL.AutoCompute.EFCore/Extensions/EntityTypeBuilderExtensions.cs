using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class EntityTypeBuilderExtensions
{
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

    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty> calculationSelector)
        where TEntity : class
    {
        return entityTypeBuilder.Property(propertyExpression)
            .AutoCompute(computedExpression, calculationSelector);
    }

    public static NavigationBuilder<TEntity, TReference> ComputedNavigation<TEntity, TReference>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TReference>> navigationExpression,
        Expression<Func<TEntity, TReference>> computedExpression)
        where TEntity : class
        where TReference : class
    {
        return entityTypeBuilder.ComputedNavigation(
            navigationExpression,
            computedExpression,
            static c => c.CurrentValue());
    }

    public static NavigationBuilder<TEntity, TProperty> ComputedNavigation<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> navigationExpression,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty> calculationSelector)
        where TEntity : class
        where TProperty : class
    {
        return entityTypeBuilder.Navigation(navigationExpression!)
            .AutoCompute(computedExpression, calculationSelector);
    }

    public static void ComputedObserver<TEntity, TValue, TChange>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<TEntity, TChange?, Task> callback)
        where TEntity : class
    {
        entityTypeBuilder
            .ComputedObserver(
                computedExpression,
                calculationSelector,
                async (ComputedChangeEventData<TEntity, TChange> eventData) =>
                {
                    foreach (var (entity, change) in eventData.Changes)
                    {
                        await callback(entity, change);
                    }
                }
            );
    }

    public static void ComputedObserver<TEntity, TValue, TChange>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<ComputedChangeEventData<TEntity, TChange>, Task> callback)
        where TEntity : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TValue>.Instance);

        entityTypeBuilder.Metadata.AddObserverFactory(
            ObserverFactory.CreateObserverFactory(
                computedExpression,
                changeCalculation,
                callback));
    }
}
