using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed.EntityContextResolvers;

public class UntrackedEntityContextResolver<TInput>(
    IStopTrackingDecision stopTrackingDecision
) : IEntityContextResolver
{
    public IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key)
    {
        if (stopTrackingDecision.ShouldStopTracking(node))
            return new UntrackedEntityContext();

        return null;
    }
}
