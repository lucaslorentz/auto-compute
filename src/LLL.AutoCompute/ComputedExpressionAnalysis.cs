using System.Collections.Concurrent;
using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalysis : IComputedExpressionAnalysis
{
    private readonly ConcurrentDictionary<Expression, HashSet<IEntityContextDefinition>> _contextsDefinitions = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<Expression, IReadOnlyDictionary<string, EntityContext>> _contexts = new(ReferenceEqualityComparer.Instance);
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
            .Add(new StaticEntityContextDefinition(key, context));
    }

    public void PropagateEntityContext(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        IEntityContextTransformer? transformer = null)
    {
        _contextsDefinitions
            .GetOrAdd(toNode, static _ => [])
            .Add(new PropagateEntityContextDefinition(this, fromNode, fromKey, toNode, toKey, transformer));
    }

    public void PropagateEntityContext(
        (Expression fromNode, string fromKey)[] fromNodesKeys,
        Expression toNode,
        string toKey,
        IEntityContextTransformer? transformer = null)
    {
        foreach (var (fromNode, fromKey) in fromNodesKeys)
        {
            PropagateEntityContext(fromNode, fromKey, toNode, toKey, transformer);
        }
    }

    public void AddAction(Action action)
    {
        _actions.Add(action);
    }

    public void RunActions()
    {
        foreach (var action in _actions)
            action();
    }

    internal void PrepareEntityContexts()
    {
        foreach (var node in _contextsDefinitions.Keys)
            GetEntityContexts(node);
    }

    public IReadOnlyDictionary<string, EntityContext> GetEntityContexts(Expression node)
    {
        return _contexts.GetOrAdd(
            node,
            (node) =>
            {
                var definitions = _contextsDefinitions.GetValueOrDefault(node, []);

                var entityContexts = definitions
                    .SelectMany(d => d.GetContexts())
                    .GroupBy(kv => kv.Key, kv => kv.Value)
                    .ToDictionary(g => g.Key, g =>
                    {
                        if (g.Count() > 1)
                            return new CompositeEntityContext(node, g.ToArray());
                        else
                            return g.First();
                    });

                return entityContexts;
            });
    }

    private interface IEntityContextDefinition
    {
        IReadOnlyDictionary<string, EntityContext> GetContexts();
    }

    private record PropagateEntityContextDefinition(
        ComputedExpressionAnalysis Analysis,
        Expression FromNode,
        string FromKey,
        Expression ToNode,
        string ToKey,
        IEntityContextTransformer? Transformer = null)
        : IEntityContextDefinition
    {
        public IReadOnlyDictionary<string, EntityContext> GetContexts()
        {
            var entityContexts = new ConcurrentDictionary<string, EntityContext>();

            var fromContexts = Analysis.GetEntityContexts(FromNode);

            foreach (var (key, entityContext) in fromContexts)
            {
                var mappedKey = MapKey(key, FromKey, ToKey);
                if (mappedKey is null)
                    continue;

                var mappedContext = Transformer is not null
                    ? Transformer.Transform(entityContext, ToNode)
                    : entityContext;

                if (!entityContexts.TryAdd(mappedKey, mappedContext))
                    throw new Exception("Key is being added to entity contexts multiple times");
            }

            return entityContexts;
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
    }

    private record StaticEntityContextDefinition(
        string Key,
        EntityContext Context
    ) : IEntityContextDefinition
    {
        public IReadOnlyDictionary<string, EntityContext> GetContexts()
        {
            return new Dictionary<string, EntityContext>
            {
                [Key] = Context
            };
        }
    }
}
