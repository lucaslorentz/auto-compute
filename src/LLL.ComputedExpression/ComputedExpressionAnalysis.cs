using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace LLL.Computed;

public class ComputedExpressionAnalysis : IComputedExpressionAnalysis
{
    private readonly ConcurrentDictionary<Expression, ConcurrentBag<Func<string, IEntityContext?>>> _entityContextProviders = new();
    private readonly ConcurrentDictionary<(Expression, string), IEntityContext> _entityContextCache = new();

    public IEntityContext ResolveEntityContext(Expression node, string key)
    {
        return _entityContextCache.GetOrAdd((node, key), _ =>
        {
            if (_entityContextProviders.TryGetValue(node, out var providers))
            {
                foreach (var provider in providers)
                {
                    var entityContext = provider(key);
                    if (entityContext is not null)
                    {
                        return entityContext;
                    }
                }
            }

            throw new Exception($"No entity context found for node '{node}' with key '{key}'");
        });
    }

    public void PropagateEntityContext(Expression fromNode, string fromKey, Expression toNode, string toKey, Func<IEntityContext, IEntityContext>? mapper = null)
    {
        AddEntityContextProvider(
            toNode,
            (key) =>
            {
                var mappedKey = MapKey(key, fromKey, toKey);
                if (mappedKey is null)
                    return null;

                var entityContext = ResolveEntityContext(fromNode, mappedKey);

                if (mapper != null)
                    entityContext = mapper(entityContext);

                return entityContext;
            });
    }

    public void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey)
    {
        foreach (var (fromNode, fromKey) in fromNodesKeys)
        {
            PropagateEntityContext(fromNode, fromKey, toNode, toKey);
        }
    }

    public void AddEntityContextProvider(
        Expression node,
        Func<string, IEntityContext?> provider)
    {
        _entityContextProviders.AddOrUpdate(
            node,
            (k) => new ConcurrentBag<Func<string, IEntityContext?>> {
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
}
