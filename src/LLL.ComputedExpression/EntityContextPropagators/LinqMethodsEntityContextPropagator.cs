using System.Linq.Expressions;

namespace L3.Computed.EntityContextPropagators;

public class LinqMethodsEntityContextPropagator
    : IEntityContextPropagator
{
    public void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(Queryable))
            {
                foreach (var arg in methodCallExpression.Arguments.Skip(1))
                {
                    if (arg is LambdaExpression lambda)
                    {
                        foreach (var param in lambda.Parameters)
                        {
                            analysis.PropagateEntityContext(
                                methodCallExpression.Arguments[0],
                                param,
                                EntityContextKeys.Element,
                                EntityContextKeys.None);
                        }
                    }
                }
            }
        }
    }
}
