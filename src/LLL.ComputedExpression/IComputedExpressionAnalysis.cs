using System.Linq.Expressions;

namespace LLL.Computed;

public interface IComputedExpressionAnalysis
{
    IEntityContext ResolveEntityContext(Expression node, string key);
    void PropagateEntityContext(Expression fromNode, string fromKey, Expression toNode, string toKey, Func<IEntityContext, IEntityContext>? mapper = null);
    void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey);
    void AddEntityContextProvider(Expression node, Func<string, IEntityContext?> provider);
}
