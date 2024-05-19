using System.Linq.Expressions;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class ConditionalEntityContextPropagator
    : IEntityContextPropagator
{
    public void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is ConditionalExpression conditionalExpression)
        {
            analysis.PropagateEntityContext(
                [
                    (conditionalExpression.IfTrue, EntityContextKeys.None),
                    (conditionalExpression.IfFalse, EntityContextKeys.None),
                ],
                node,
                EntityContextKeys.None
            );
        }
    }
}