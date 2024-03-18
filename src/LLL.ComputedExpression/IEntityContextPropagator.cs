using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityContextPropagator
{
    void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis);
}
