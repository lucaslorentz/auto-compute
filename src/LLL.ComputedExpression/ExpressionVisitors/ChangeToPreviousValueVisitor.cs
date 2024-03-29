using System.Linq.Expressions;

namespace LLL.Computed.ExpressionVisitors;

internal class ChangeToPreviousValueVisitor(
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
                    return propertyAccess.Member.CreatePreviousValueExpression(
                        propertyAccess,
                        inputExpression);
            }
        }

        return base.Visit(node);
    }
}
