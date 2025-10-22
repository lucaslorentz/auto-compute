using System.Linq.Expressions;
using LLL.AutoCompute.Internal.ExpressionVisitors;

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

            foreach (var expression in CollectExpressionsVisitor.Collect(conditionalExpression.Test))
            {
                entityContextRegistry.RegisterModifier(
                    expression,
                    entityContexts =>
                    {
                        foreach (var entityContext in entityContexts.Values)
                            entityContext.MarkNavigationToLoadAll();
                    }
                );
            }
        }
    }
}
