using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedMemberAccessLocator(IModel model) :
    IObservedNavigationAccessLocator<IEFCoreComputedInput>,
    IObservedPropertyAccessLocator<IEFCoreComputedInput>
{
    public virtual IObservedMemberAccess<IObservedNavigation>? GetObservedNavigationAccess(Expression node)
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
                return ObserbedMemberAccess.Create(node, entityExpression, GetNavigation(navigation));
        }

        return null;
    }

    public virtual IObservedMemberAccess<IObservedProperty>? GetObservedPropertyAccess(Expression node)
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
                return ObserbedMemberAccess.Create(node, entityExpression, GetProperty(property));
        }

        return null;
    }

    protected virtual IObservedNavigation GetNavigation(INavigationBase navigation)
    {
        return navigation.GetOrCreateObservedNavigation();
    }

    protected virtual IObservedProperty GetProperty(IProperty property)
    {
        return property.GetOrCreateObservedProperty();
    }
}
