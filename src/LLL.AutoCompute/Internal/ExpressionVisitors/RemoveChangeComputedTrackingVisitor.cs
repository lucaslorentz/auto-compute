using System.Linq.Expressions;
using LLL.AutoCompute;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

public class RemoveChangeComputedTrackingVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(ChangeTrackingExtensions)
            && (
                node.Method.Name == nameof(ChangeTrackingExtensions.AsComputedUntracked)
                || node.Method.Name == nameof(ChangeTrackingExtensions.AsComputedTracked)
            ))
        {
            return node.Arguments[0];
        }

        return base.VisitMethodCall(node);
    }
}
