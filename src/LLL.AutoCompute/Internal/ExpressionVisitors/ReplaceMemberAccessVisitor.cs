using System.Linq.Expressions;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

internal class ReplaceMemberAccessVisitor(
    IReadOnlyCollection<IObservedMemberAccessLocator> memberAccessLocators,
    Func<IObservedMemberAccess<IObservedMember>, Expression> expressionModifier
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var memberAccessLocator in memberAccessLocators)
            {
                var memberAccess = memberAccessLocator.GetObservedMemberAccess(node);
                if (memberAccess is not null)
                {
                    var modifiedNode = expressionModifier(memberAccess);

                    return base.Visit(modifiedNode);
                }
            }
        }

        return base.Visit(node);
    }
}
