using System.Linq.Expressions;
using LLL.AutoCompute.EntityContextTransformers;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ChangeTrackingEntityContextPropagator : IEntityContextPropagator
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
                    new ChangeTrackingEntityContextTransformer(node, false));
            }
            else if (methodCallExpression.Method.Name == nameof(ChangeTrackingExtensions.AsComputedTracked))
            {
                analysis.PropagateEntityContext(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    new ChangeTrackingEntityContextTransformer(node, true));
            }
        }
    }
}
