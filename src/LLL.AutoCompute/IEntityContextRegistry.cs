using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IEntityContextRegistry
{
    void RegisterContext(Expression node, string key, EntityContext context);
    void RegisterPropagation(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? transform = null);
    void RegisterPropagation(
        (Expression fromNode, string fromKey)[] fromNodesKeys,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? transform = null);
    void RegisterModifier(Expression node, string key, Action<EntityContext> modifier);
}