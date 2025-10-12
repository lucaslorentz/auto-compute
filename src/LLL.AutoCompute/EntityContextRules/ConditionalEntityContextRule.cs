using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ConditionalEntityContextRule
    : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry)
    {
        if (node is ConditionalExpression conditionalExpression)
        {
            entityContextRegistry.RegisterPropagation(
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