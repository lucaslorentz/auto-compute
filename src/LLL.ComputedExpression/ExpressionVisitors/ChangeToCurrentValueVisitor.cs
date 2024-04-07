using System.Linq.Expressions;

namespace LLL.ComputedExpression.ExpressionVisitors;

internal class ChangeToCurrentValueVisitor(
    ParameterExpression inputExpression,
    IReadOnlyCollection<IEntityMemberAccessLocator> memberAccessLocators
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var propertyAccessLocator in memberAccessLocators)
            {
                var propertyAccess = propertyAccessLocator.GetEntityMemberAccess(node);
                if (propertyAccess is not null)
                    return propertyAccess.Member.CreateCurrentValueExpression(
                        propertyAccess,
                        inputExpression);
            }
        }

        return base.Visit(node);
    }
}
