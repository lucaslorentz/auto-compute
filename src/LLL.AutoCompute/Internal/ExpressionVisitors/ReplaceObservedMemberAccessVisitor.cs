using System.Linq.Expressions;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

internal class ReplaceObservedMemberAccessVisitor(
    IReadOnlyCollection<IObservedMemberAccessLocator> memberAccessLocators,
    Func<ObservedMemberAccess, Expression> expressionModifier
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        var visitedNode = base.Visit(node);

        if (visitedNode is not null)
        {
            foreach (var memberAccessLocator in memberAccessLocators)
            {
                var memberAccess = memberAccessLocator.GetObservedMemberAccess(visitedNode);
                if (memberAccess is not null)
                    return expressionModifier(memberAccess);
            }
        }

        return visitedNode;
    }
}
