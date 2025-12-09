using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

/// <summary>
/// Registry for entity contexts during expression analysis. Manages context propagation
/// and modifications as the expression tree is traversed.
/// </summary>
public interface IEntityContextRegistry
{
    /// <summary>Registers an entity context for an expression node with a given key.</summary>
    void RegisterContext(Expression node, string key, EntityContext context);

    /// <summary>Registers propagation of an entity context from one node to another.</summary>
    void RegisterPropagation(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? transform = null);

    /// <summary>Registers propagation from multiple source nodes to a target node.</summary>
    void RegisterPropagation(
        (Expression fromNode, string fromKey)[] fromNodesKeys,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? transform = null);

    /// <summary>Registers a modifier that is executed when the context becomes available.</summary>
    void RegisterRequiredModifier(Expression node, string key, Action<EntityContext> modifier);

    /// <summary>Registers a modifier that is executed only if the context exists.</summary>
    void RegisterOptionalModifier(Expression node, string key, Action<EntityContext> modifier);

    /// <summary>Registers a modifier that receives all contexts for a node.</summary>
    void RegisterModifier(Expression node, Action<IReadOnlyDictionary<string, EntityContext>> modifier);
}
