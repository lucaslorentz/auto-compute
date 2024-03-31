using System.Linq.Expressions;
using LLL.Computed.EFCore.Internal;
using LLL.Computed.Incremental;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.Computed.EFCore;

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
        TProperty initialValue,
        Action<IncrementalComputedBuilder<TEntity, TProperty>> build)
        where TEntity : class
    {
        var propertyBuilder = entityTypeBuilder.Property(propertyExpression);
        var incrementalComputedBuilder = new IncrementalComputedBuilder<TEntity, TProperty>(initialValue);
        build(incrementalComputedBuilder);
        propertyBuilder.HasAnnotation(ComputedAnnotationNames.Expression, incrementalComputedBuilder);
        return propertyBuilder;
    }
}
