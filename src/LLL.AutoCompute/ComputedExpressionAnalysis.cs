using System.Collections.Concurrent;
using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalysis : IComputedExpressionAnalysis
{
    private readonly ConcurrentDictionary<Expression, ConcurrentBag<Func<IReadOnlyDictionary<string, EntityContext>>>> _contextsDefinitions = new();
    private readonly ConcurrentDictionary<Expression, IReadOnlyDictionary<string, EntityContext>> _contexts = new();
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
        _contextsDefinitions
            .GetOrAdd(node, static _ => [])
            .Add(() => new Dictionary<string, EntityContext>
            {
                [key] = context
            });
    }

    public void PropagateEntityContext(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? mapper = null)
    {
        _contextsDefinitions
            .GetOrAdd(toNode, static _ => [])
            .Add(() =>
            {
                var entityContexts = new ConcurrentDictionary<string, EntityContext>();

                var fromContexts = RunNodeDefinitions(fromNode);

                foreach (var (key, entityContext) in fromContexts)
                {
                    var mappedKey = MapKey(key, fromKey, toKey);
                    if (mappedKey is null)
                        continue;

                    var mappedContext = mapper is not null
                        ? mapper(entityContext)
                        : entityContext;

                    if (!entityContexts.TryAdd(mappedKey, mappedContext))
                        throw new Exception("Key is being added to entity contexts multiple times");
                }

                return entityContexts;
            });
    }

    public void PropagateEntityContext(
        (Expression fromNode, string fromKey)[] fromNodesKeys,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? mapper = null)
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

    internal void PrepareEntityContexts()
    {
        foreach (var key in _contextsDefinitions.Keys)
            RunNodeDefinitions(key);
    }

    private IReadOnlyDictionary<string, EntityContext> RunNodeDefinitions(Expression node)
    {
        return _contexts.GetOrAdd(
            node,
            (node) =>
            {
                var definitions = _contextsDefinitions.GetValueOrDefault(node, []);

                var entityContexts = definitions
                    .SelectMany(d => d())
                    .GroupBy(kv => kv.Key, kv => kv.Value)
                    .ToDictionary(g => g.Key, g =>
                    {
                        if (g.Count() > 1)
                            return new CompositeEntityContext(g.ToArray());
                        else
                            return g.First();
                    });

                return entityContexts;
            });
    }
}
