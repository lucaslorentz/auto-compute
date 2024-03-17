using System.Linq.Expressions;

namespace L3.Computed;

public interface IStopTrackingDecision
{
    bool ShouldStopTracking(Expression node);
}
