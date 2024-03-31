using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IEntityContextPropagator
{
    void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis);
}
