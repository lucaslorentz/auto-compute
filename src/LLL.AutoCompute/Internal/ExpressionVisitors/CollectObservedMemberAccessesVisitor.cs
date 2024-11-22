using System.Linq.Expressions;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

internal class CollectObservedMemberAccessesVisitor(
    IComputedExpressionAnalysis analysis,
    IReadOnlyCollection<IObservedMemberAccessLocator> memberAccessLocators
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
                    var entityContext = analysis.ResolveEntityContext(memberAccess.FromExpression, EntityContextKeys.None);
                    if (entityContext.IsTrackingChanges)
                        entityContext.RegisterAccessedMember(memberAccess.Member);
                }
            }
        }

        return base.Visit(node);
    }
}
