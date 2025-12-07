using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedMemberAccessLocator(IModel model) :
    IObservedMemberAccessLocator
{
    public virtual ObservedMemberAccess? GetObservedMemberAccess(Expression node)
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

            var property = entityType?.FindProperty(memberExpression.Member);
            if (property is not null)
            {
                return new ObservedMemberAccess(
                    node,
                    entityExpression,
                    property.GetOrCreateObservedProperty());
            }

            var navigation = (INavigationBase?)entityType?.FindNavigation(memberExpression.Member)
                ?? entityType?.FindSkipNavigation(memberExpression.Member);
            if (navigation is not null)
            {
                return new ObservedMemberAccess(
                    node,
                    entityExpression,
                    navigation.GetOrCreateObservedNavigation());
            }
        }

        return null;
    }
}
