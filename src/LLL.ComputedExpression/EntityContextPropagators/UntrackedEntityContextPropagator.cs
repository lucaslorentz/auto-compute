using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class UntrackedEntityContextPropagator<TInput> : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (node is MethodCallExpression methodCallExpression
            && methodCallExpression.Method.DeclaringType == typeof(UntrackedExtensions))
        {
            if (methodCallExpression.Method.Name == nameof(UntrackedExtensions.AsComputedUntracked))
            {
                analysis.PropagateEntityContext(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    (e) => new UntrackedEntityContext(node.Type, e));
            }
            else if (methodCallExpression.Method.Name == nameof(UntrackedExtensions.AsComputedTracked))
            {
                analysis.PropagateEntityContext(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    (e) => new TrackedEntityContext(node.Type, e));
            }
        }
    }
}
