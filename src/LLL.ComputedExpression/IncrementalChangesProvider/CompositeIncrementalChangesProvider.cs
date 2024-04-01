
using System.Collections.Concurrent;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProvider;

public class CompositeIncrementalChangesProvider(
    IIncrementalComputed incrementalComputed,
    IReadOnlyCollection<IIncrementalChangesProvider> providers
) : IIncrementalChangesProvider
{
    public async Task<IDictionary<object, object?>> GetIncrementalChangesAsync(object input)
    {
        var aggregated = new ConcurrentDictionary<object, object?>();

        foreach (var provider in providers)
        {
            var incrementalChanges = await provider.GetIncrementalChangesAsync(input);
            foreach (var kv in incrementalChanges)
            {
                aggregated.AddOrUpdate(
                    kv.Key,
                    (k) => kv.Value,
                    (k, v) => incrementalComputed.Add(v, kv.Value)
                );
            }
        }

        foreach (var kv in aggregated)
        {
            if (incrementalComputed.IsZero(kv.Value))
                aggregated.Remove(kv.Key, out var _);
        }

        return aggregated;
    }
}