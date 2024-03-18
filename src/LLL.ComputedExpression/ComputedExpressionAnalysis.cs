using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace LLL.Computed;

public class ComputedExpressionAnalysis
    : IComputedExpressionAnalysis
{
    private readonly IList<IEntityContextResolver> _entityContextResolvers;

    internal ComputedExpressionAnalysis(
        IList<IEntityContextResolver> entityContextResolvers
    )
    {
        _entityContextResolvers = entityContextResolvers;
    }

    private readonly ConcurrentDictionary<Expression, Func<string, IEntityContext?>> _entityContextProviders = new();
    private readonly ConcurrentDictionary<(Expression, string), IEntityContext> _entityContextCache = new();

    public IEntityContext ResolveEntityContext(Expression node, string key)
    {
        return _entityContextCache.GetOrAdd((node, key), _ =>
        {
            if (_entityContextProviders.TryGetValue(node, out var provider))
            {
                var entityContext = provider(key);
                if (entityContext is not null)
                {
                    return entityContext;
                }
            }

            foreach (var entityContextResolver in _entityContextResolvers)
            {
                var entityContext = entityContextResolver.ResolveEntityContext(node, this, key);
                if (entityContext is not null)
                {
                    return entityContext;
                }
            }

            throw new Exception($"No entity context found for node '{node}' with key '{key}'");
        });
    }

    public void PropagateEntityContext(
        Expression fromNode,
        Expression toNode,
        string fromKey,
        string toKey)
    {
        AddEntityContextProvider(
            toNode,
            (key) =>
            {
                var mappedKey = MapKey(key, fromKey, toKey);
                if (mappedKey is null)
                    return null;
                return ResolveEntityContext(fromNode, mappedKey);
            });
    }

    private static string? MapKey(string key, string fromKey, string toKey)
    {
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

        return "/" + string.Join("/", finalParts);
    }

    internal ComputedExpressionAnalysis AddEntityContextProvider(
        Expression node,
        Func<string, IEntityContext?> provider)
    {
        _entityContextProviders.TryAdd(node, provider);
        return this;
    }
}
