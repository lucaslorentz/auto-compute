using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IEntityContextPropagator
{
    void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis);
}
