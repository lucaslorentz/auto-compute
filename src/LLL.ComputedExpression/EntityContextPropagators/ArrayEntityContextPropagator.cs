using System.Linq.Expressions;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class ArrayEntityContextPropagator
    : IEntityContextPropagator
{
    public void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is NewArrayExpression newArrayExpression)
        {
            analysis.PropagateEntityContext(
                newArrayExpression.Expressions
                    .Select(e => (e, EntityContextKeys.None))
                    .ToArray(),
                node,
                EntityContextKeys.Element
            );
        }
    }
}