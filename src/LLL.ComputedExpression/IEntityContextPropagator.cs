using System.Linq.Expressions;

namespace L3.Computed;

public interface IEntityContextPropagator
{
    void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis);
}
