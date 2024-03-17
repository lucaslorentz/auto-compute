using System.Linq.Expressions;

namespace L3.Computed;

public interface IComputedExpressionAnalysis
{
    IEntityContext ResolveEntityContext(Expression node, string key);
    void PropagateEntityContext(Expression fromNode, Expression toNode, string fromKey, string toKey);
}
