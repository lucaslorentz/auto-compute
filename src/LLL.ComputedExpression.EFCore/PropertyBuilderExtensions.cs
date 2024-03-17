using System.Linq.Expressions;
using L3.Computed.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace L3.Computed.EFCore;

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
}
