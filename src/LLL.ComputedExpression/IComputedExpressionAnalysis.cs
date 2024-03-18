using System.Linq.Expressions;

namespace LLL.Computed;

public interface IComputedExpressionAnalysis
{
    IEntityContext ResolveEntityContext(Expression node, string key);
    void PropagateEntityContext(Expression fromNode, Expression toNode, string fromKey, string toKey);
}
