using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ArrayEntityContextRule
    : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextBuilder)
    {
        if (node is NewArrayExpression newArrayExpression)
        {
            entityContextBuilder.RegisterPropagation(
                newArrayExpression.Expressions
                    .Select(e => (e, EntityContextKeys.None))
                    .ToArray(),
                node,
                EntityContextKeys.Element
            );
        }
    }
}