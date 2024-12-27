using System.Collections.Concurrent;
using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalysis : IComputedExpressionAnalysis
{
    private record Propagation(Expression FromNode, string FromKey, Expression ToNode, string ToKey, Func<EntityContext, EntityContext>? Mapper);

    private readonly ConcurrentDictionary<Expression, ConcurrentDictionary<string, EntityContext>> _contexts = new();
    private readonly ConcurrentDictionary<Expression, ConcurrentBag<Propagation>> _toPropagations = new();
    private readonly ConcurrentBag<Action> _actions = [];

    public EntityContext ResolveEntityContext(Expression node, string key)
    {
        return _contexts.TryGetValue(node, out var nodeContexts)
            && nodeContexts.TryGetValue(key, out var entityContext)
            ? entityContext
            : throw new Exception($"No entity context found for node '{node}' with key '{key}'");
    }

    public void AddContext(
        Expression node,
        string key,
        EntityContext context)
    {
        var nodeContexts = _contexts.GetOrAdd(node, static k => []);
        AddNodeContext(nodeContexts, key, context);
    }

    public void PropagateEntityContext(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? mapper = null)
    {
        var propagation = new Propagation(fromNode, fromKey, toNode, toKey, mapper);
        _toPropagations.GetOrAdd(toNode, static _ => []).Add(propagation);
    }

    public void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null)
    {
        foreach (var (fromNode, fromKey) in fromNodesKeys)
        {
            PropagateEntityContext(fromNode, fromKey, toNode, toKey, mapper);
        }
    }

    public void AddAction(Action action)
    {
        _actions.Add(action);
    }

    private static string? MapKey(string key, string fromKey, string toKey)
    {
        if (!key.StartsWith(fromKey))
            return null;

        var keyParts = key.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
        var fromParts = fromKey.Split("/", StringSplitOptions.RemoveEmptyEntries);
        var toParts = toKey.Split("/", StringSplitOptions.RemoveEmptyEntries);

        foreach (var fromPart in fromParts)
        {
            if (keyParts[0] != fromPart)
                return null;

            keyParts.RemoveAt(0);
        }

        var finalParts = toParts.Concat(keyParts);
        if (!finalParts.Any())
            return "";

        return "/" + string.Join("/", finalParts);
    }

    public void RunActions()
    {
        foreach (var action in _actions)
            action();
    }

    internal void RunPropagations()
    {
        foreach (var key in _toPropagations.Keys)
            PropagateToNode(key);
    }

    private IReadOnlyDictionary<string, EntityContext> PropagateToNode(Expression node)
    {
        return _contexts.GetOrAdd(
            node,
            (node) =>
            {
                var entityContexts = new ConcurrentDictionary<string, EntityContext>();

                foreach (var propagation in _toPropagations.GetValueOrDefault(node, []))
                {
                    var fromContexts = PropagateToNode(propagation.FromNode);

                    foreach (var (key, entityContext) in fromContexts)
                    {
                        var mappedKey = MapKey(key, propagation.FromKey, propagation.ToKey);
                        if (mappedKey is null)
                            continue;

                        var mappedContext = propagation.Mapper is not null
                            ? propagation.Mapper(entityContext)
                            : entityContext;

                        AddNodeContext(entityContexts, mappedKey, mappedContext);
                    }
                }

                return entityContexts;
            });
    }

    private static void AddNodeContext(ConcurrentDictionary<string, EntityContext> nodeContexts, string key, EntityContext context)
    {
        nodeContexts.AddOrUpdate(
            key,
            static (key, context) => context,
            static (key, existing, context) =>
            {
                if (existing is CompositeEntityContext compositeEntityContext)
                {
                    compositeEntityContext.AddParent(context);
                    return compositeEntityContext;
                }
                else if (existing is not null)
                {
                    return new CompositeEntityContext([existing, context]);
                }
                else
                {
                    return context;
                }
            },
            context
        );
    }
}
