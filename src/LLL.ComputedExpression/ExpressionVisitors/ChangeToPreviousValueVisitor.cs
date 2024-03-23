using System.Linq.Expressions;

namespace LLL.Computed.ExpressionVisitors;

internal class ChangeToPreviousValueVisitor(
    ICollection<IEntityMemberAccessLocator> memberAccessLocators
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
                    return propertyAccess.Member.CreatePreviousValueExpression(propertyAccess);
            }
        }

        return base.Visit(node);
    }
}
