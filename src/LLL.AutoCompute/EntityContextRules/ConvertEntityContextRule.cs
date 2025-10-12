using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ConvertEntityContextRule
    : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry)
    {
        if (node is UnaryExpression unaryExpression
            && (
                unaryExpression.NodeType == ExpressionType.Convert
                || unaryExpression.NodeType == ExpressionType.ConvertChecked
            ))
        {
            entityContextRegistry.RegisterPropagation(
                unaryExpression.Operand,
                EntityContextKeys.None,
                node,
                EntityContextKeys.None
            );
        }
    }
}