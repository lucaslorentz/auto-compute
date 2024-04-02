using System.Linq.Expressions;
using System.Numerics;
using LLL.ComputedExpression.EFCore.Internal;
using LLL.ComputedExpression.Incremental;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.ComputedExpression.EFCore;

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<TProperty> ComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Expression<Func<TEntity, TProperty>> computedExpression)
        where TEntity : class
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);
        propertyBuilder.HasAnnotation(ComputedAnnotationNames.Expression, computedExpression);
        return propertyBuilder;
    }

    public static PropertyBuilder<TProperty> IncrementalComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        Action<NumberIncrementalComputed<TEntity, TProperty>> buildComputed)
        where TEntity : class
        where TProperty : INumber<TProperty>
    {
        var incrementalComputed = new NumberIncrementalComputed<TEntity, TProperty>();
        buildComputed(incrementalComputed);
        return entityTypeBuilder.IncrementalComputedProperty(propertyExpression, incrementalComputed);
    }

    public static PropertyBuilder<TProperty> IncrementalComputedProperty<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        IIncrementalComputed<TEntity, TProperty> incrementalComputed)
        where TEntity : class
        where TProperty : INumber<TProperty>
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);
        propertyBuilder.HasAnnotation(ComputedAnnotationNames.Expression, incrementalComputed);
        return propertyBuilder;
    }
}
