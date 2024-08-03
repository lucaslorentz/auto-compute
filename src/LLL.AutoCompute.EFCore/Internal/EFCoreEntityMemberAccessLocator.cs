using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreEntityMemberAccessLocator(IModel model) :
    IEntityNavigationAccessLocator<IEFCoreComputedInput>,
    IEntityPropertyAccessLocator<IEFCoreComputedInput>
{
    public virtual IEntityMemberAccess<IEntityNavigation>? GetEntityNavigationAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var entityExpression = memberExpression.Expression.Type.IsInterface
                && memberExpression.Expression is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert
                ? unaryExpression.Operand
                : memberExpression.Expression;

            var entityType = model.FindEntityType(entityExpression.Type);
            var navigation = (INavigationBase?)entityType?.FindNavigation(memberExpression.Member)
                ?? entityType?.FindSkipNavigation(memberExpression.Member);
            if (navigation != null)
                return EntityMemberAccess.Create(node, entityExpression, GetNavigation(navigation));
        }

        return null;
    }

    public virtual IEntityMemberAccess<IEntityProperty>? GetEntityPropertyAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var entityExpression = memberExpression.Expression.Type.IsInterface
                && memberExpression.Expression is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert
                ? unaryExpression.Operand
                : memberExpression.Expression;

            var type = entityExpression.Type;
            var entityType = model.FindEntityType(type);
            var property = entityType?.FindProperty(memberExpression.Member);
            if (property is not null)
                return EntityMemberAccess.Create(node, entityExpression, GetProperty(property));
        }

        return null;
    }

    protected virtual IEntityNavigation GetNavigation(INavigationBase navigation)
    {
        return navigation.GetEntityNavigation();
    }

    protected virtual IEntityProperty GetProperty(IProperty property)
    {
        return property.GetEntityProperty();
    }
}
