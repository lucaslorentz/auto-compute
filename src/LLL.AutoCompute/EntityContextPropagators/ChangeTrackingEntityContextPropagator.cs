using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ChangeTrackingEntityContextPropagator<TInput> : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (node is MethodCallExpression methodCallExpression
            && methodCallExpression.Method.DeclaringType == typeof(ChangeTrackingExtensions))
        {
            if (methodCallExpression.Method.Name == nameof(ChangeTrackingExtensions.AsComputedUntracked))
            {
                analysis.PropagateEntityContext(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    (e) => new ChangeTrackingEntityContext(node.Type, false, e));
            }
            else if (methodCallExpression.Method.Name == nameof(ChangeTrackingExtensions.AsComputedTracked))
            {
                analysis.PropagateEntityContext(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    (e) => new ChangeTrackingEntityContext(node.Type, true, e));
            }
        }
    }
}
