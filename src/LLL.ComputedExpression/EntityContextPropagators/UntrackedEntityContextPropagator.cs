using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.EntityContextPropagators;

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
