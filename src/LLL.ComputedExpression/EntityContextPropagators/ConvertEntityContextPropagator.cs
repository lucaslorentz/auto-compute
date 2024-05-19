using System.Linq.Expressions;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class ConvertEntityContextPropagator
    : IEntityContextPropagator
{
    public void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is UnaryExpression unaryExpression
            && (
                unaryExpression.NodeType == ExpressionType.Convert
                || unaryExpression.NodeType == ExpressionType.ConvertChecked
            ))
        {
            analysis.PropagateEntityContext(
                unaryExpression.Operand,
                EntityContextKeys.None,
                node,
                EntityContextKeys.None
            );
        }
    }
}