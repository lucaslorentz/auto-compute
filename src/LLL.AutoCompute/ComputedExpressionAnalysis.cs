using System.Collections.Concurrent;
using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalysis : IComputedExpressionAnalysis
{
    private readonly ConcurrentDictionary<Expression, ConcurrentBag<Func<string, EntityContext?>>> _entityContextProviders = new();
    private readonly ConcurrentDictionary<(Expression, string), EntityContext> _entityContextCache = new();
    private readonly ConcurrentDictionary<Expression, IEntityMemberAccess<IEntityMember>> _entityMemberAccesses = new();
    private readonly ConcurrentBag<Action> _incrementalActions = [];

    public EntityContext ResolveEntityContext(Expression node, string key)
    {
        return _entityContextCache.GetOrAdd((node, key), _ =>
        {
            if (_entityContextProviders.TryGetValue(node, out var providers))
            {
                foreach (var provider in providers)
                {
                    var entityContext = provider(key);
                    if (entityContext is not null)
                        return entityContext;
                }
            }

            throw new Exception($"No entity context found for node '{node}' with key '{key}'");
        });
    }

    public void PropagateEntityContext(
        Expression fromNode,
        string fromKey,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? mapper = null)
    {
        PropagateEntityContext([(fromNode, fromKey)], toNode, toKey, mapper);
    }

    public void PropagateEntityContext(
        (Expression fromNode, string fromKey)[] fromNodesKeys,
        Expression toNode,
        string toKey,
        Func<EntityContext, EntityContext>? mapper = null)
    {
        AddEntityContextProvider(
            toNode,
            (key) =>
            {
                var entityContexts = new List<EntityContext>();

                foreach (var (fromNode, fromKey) in fromNodesKeys)
                {
                    var mappedKey = MapKey(key, fromKey, toKey);
                    if (mappedKey is null)
                        return null;

                    var entityContext = ResolveEntityContext(fromNode, mappedKey);

                    if (mapper != null)
                        entityContext = mapper(entityContext);

                    entityContexts.Add(entityContext);
                }

                if (entityContexts.Count > 1)
                    return new CompositeEntityContext(entityContexts);

                return entityContexts.FirstOrDefault();
            });
    }

    public void AddIncrementalAction(Action action)
    {
        _incrementalActions.Add(action);
    }

    public void AddEntityContextProvider(
        Expression node,
        Func<string, EntityContext?> provider)
    {
        _entityContextProviders.AddOrUpdate(
            node,
            (k) => new ConcurrentBag<Func<string, EntityContext?>> {
                provider
            },
            (k, providers) =>
            {
                providers.Add(provider);
                return providers;
            });
    }

    private static string? MapKey(string key, string fromKey, string toKey)
    {
        if (!key.StartsWith(toKey))
            return null;

        var keyParts = key.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
        var fromParts = fromKey.Split("/", StringSplitOptions.RemoveEmptyEntries);
        var toParts = toKey.Split("/", StringSplitOptions.RemoveEmptyEntries);

        foreach (var toPart in toParts)
        {
            if (keyParts[0] != toPart)
                return null;

            keyParts.RemoveAt(0);
        }

        var finalParts = fromParts.Concat(keyParts);
        if (!finalParts.Any())
            return "";

        var result = "/" + string.Join("/", finalParts);

        return result;
    }

    public void AddMemberAccess(Expression node, IEntityMemberAccess<IEntityMember> entityMemberAccess)
    {
        _entityMemberAccesses.TryAdd(node, entityMemberAccess);
    }

    public void RunIncrementalActions()
    {
        foreach (var action in _incrementalActions)
            action();
    }
}
