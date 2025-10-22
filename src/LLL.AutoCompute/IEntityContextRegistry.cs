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
    void RegisterRequiredModifier(Expression node, string key, Action<EntityContext> modifier);
    void RegisterOptionalModifier(Expression node, string key, Action<EntityContext> modifier);
    void RegisterModifier(Expression node, Action<IReadOnlyDictionary<string, EntityContext>> modifier);
}