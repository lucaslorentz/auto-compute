using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IStopTrackingDecision
{
    bool ShouldStopTracking(Expression node);
}
