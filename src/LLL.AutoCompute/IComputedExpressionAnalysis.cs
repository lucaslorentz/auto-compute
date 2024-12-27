using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IComputedExpressionAnalysis
{
    EntityContext ResolveEntityContext(Expression node, string key);
    void AddContext(Expression node, string key, EntityContext context);
    void PropagateEntityContext(Expression fromNode, string fromKey, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void AddAction(Action action);
}
