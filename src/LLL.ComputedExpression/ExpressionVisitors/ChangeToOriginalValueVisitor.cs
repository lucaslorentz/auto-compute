using System.Linq.Expressions;

namespace LLL.ComputedExpression.ExpressionVisitors;

internal class ChangeToOriginalValueVisitor(
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
                    return propertyAccess.Member.CreateOriginalValueExpression(
                        propertyAccess,
                        inputExpression);
            }
        }

        return base.Visit(node);
    }
}
