using System.Linq.Expressions;

namespace LLL.Computed;

public interface IStopTrackingDecision
{
    bool ShouldStopTracking(Expression node);
}
