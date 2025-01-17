﻿using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.AutoCompute.EFCore;

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty>? calculationSelector = null)
        where TEntity : class
    {
        return entityTypeBuilder.Property(propertyExpression)
            .AutoCompute(computedExpression, calculationSelector);
    }

    public static NavigationBuilder<TEntity, TProperty> ComputedNavigation<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> navigationExpression,
        Expression<Func<TEntity, TProperty>> computedExpression,
        ChangeCalculationSelector<TProperty, TProperty>? calculationSelector = null,
        Action<IComputedNavigationBuilder<TEntity, TProperty>>? configure = null)
        where TEntity : class
        where TProperty : class
    {
        return entityTypeBuilder.Navigation(navigationExpression!)
            .AutoCompute(computedExpression, calculationSelector, configure);
    }

    public static void ComputedObserver<TEntity, TValue, TChange>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<TEntity, TChange?, Task> callback)
        where TEntity : class
    {
        entityTypeBuilder
            .ComputedObserver(
                computedExpression,
                filterExpression,
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
        Expression<Func<TEntity, bool>>? filterExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector,
        Func<ComputedChangeEventData<TEntity, TChange>, Task> callback)
        where TEntity : class
    {
        var changeCalculation = calculationSelector(ChangeCalculations<TValue>.Instance);

        entityTypeBuilder.Metadata.AddObserverFactory(
            ObserverFactory.CreateObserverFactory(
                computedExpression,
                filterExpression,
                changeCalculation,
                callback));
    }
}
