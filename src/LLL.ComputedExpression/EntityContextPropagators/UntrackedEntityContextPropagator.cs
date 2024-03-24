using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed.EntityContextPropagators;

public class UntrackedEntityContextPropagator(
    IStopTrackingDecision stopTrackingDecision
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (stopTrackingDecision.ShouldStopTracking(node))
            analysis.AddEntityContextProvider(node, (key) => new UntrackedEntityContext());
    }
}
