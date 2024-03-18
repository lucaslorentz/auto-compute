using System.Linq.Expressions;

namespace LLL.Computed.ExpressionVisitors;

internal class PropagateEntityContextsVisitor(
    IList<IEntityContextPropagator> propagators,
    IComputedExpressionAnalysis analysis
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var propagator in propagators)
                propagator.PropagateEntityContext(node, analysis);
        }

        return base.Visit(node);
    }
}
