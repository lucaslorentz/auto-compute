using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public class StopTrackingDecision : IStopTrackingDecision
{
    public bool ShouldStopTracking(Expression node)
    {
        return node is MethodCallExpression methodCallExpression
            && methodCallExpression.Method.DeclaringType == typeof(StopTrackingExtensions)
            && methodCallExpression.Method.Name == nameof(StopTrackingExtensions.AsComputedUntracked);
    }
}
