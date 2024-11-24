using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public static class ComputedNavigationBuilderExtensions
{
    public static void ReuseAndUpdateItems<TEntity, TTarget>(
        this IComputedNavigationBuilder<TEntity, IEnumerable<TTarget>> builder,
        Expression<Func<TTarget, object>> keySelector,
        Expression<Func<TTarget, object>> updatePropertiesSelector)
    {
        builder.ReuseKeySelector = keySelector.Compile();
        foreach (var updateProperty in FindProperties(builder.Property.TargetEntityType, updatePropertiesSelector))
            builder.ReuseUpdateProperties.Add(updateProperty);
    }

    public static void ReuseAndUpdateItems<TEntity, TTarget>(
        this IComputedNavigationBuilder<TEntity, TTarget> builder,
        Expression<Func<TTarget, object>> keySelector,
        Expression<Func<TTarget, object>> updatePropertiesSelector)
    {
        builder.ReuseKeySelector = keySelector.Compile();
        foreach (var updateProperty in FindProperties(builder.Property.TargetEntityType, updatePropertiesSelector))
            builder.ReuseUpdateProperties.Add(updateProperty);
    }

    private static IEnumerable<IProperty> FindProperties<TEntity>(
        IEntityType entityType,
        Expression<Func<TEntity, object>> selector)
    {
        var body = selector.Body;
        if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
            body = unaryExpression.Operand;

        if (body is MemberExpression memberExpression)
        {
            yield return entityType.FindProperty(memberExpression.Member)
                ?? throw new Exception($"Property not found for member {memberExpression.Member}");
        }
        else if (body is MemberInitExpression memberInitExpression)
        {
            foreach (var binding in memberInitExpression.Bindings)
            {
                if (binding is MemberAssignment memberAssignment
                    && memberAssignment.Expression is MemberExpression subMemberExpression)
                {
                    yield return entityType.FindProperty(subMemberExpression.Member)
                        ?? throw new Exception($"Property not found for member {subMemberExpression.Member}");
                }
                else throw new Exception($"Unsupported property expression {selector}");
            }
        }
        else
        {
            throw new Exception($"Unsupported property expression {selector}");
        }
    }
}

