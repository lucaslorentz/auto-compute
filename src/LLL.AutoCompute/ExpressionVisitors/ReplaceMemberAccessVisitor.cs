using System.Linq.Expressions;

namespace LLL.AutoCompute.ExpressionVisitors;

internal class ReplaceMemberAccessVisitor(
    IReadOnlyCollection<IEntityMemberAccessLocator> memberAccessLocators,
    Func<IEntityMemberAccess<IEntityMember>, Expression> expressionModifier
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var memberAccessLocator in memberAccessLocators)
            {
                var memberAccess = memberAccessLocator.GetEntityMemberAccess(node);
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
