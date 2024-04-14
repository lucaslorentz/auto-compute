
using System.Collections.Concurrent;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProviders;

public class CompositeIncrementalChangesProvider<TInput, TRootEntity, TValue>(
    IIncrementalComputed<TRootEntity, TValue> incrementalComputed,
    IReadOnlyCollection<IIncrementalChangesProvider<TInput, TRootEntity, TValue>> providers
) : IIncrementalChangesProvider<TInput, TRootEntity, TValue>
    where TRootEntity : notnull
{
    public async Task<IReadOnlyDictionary<TRootEntity, TValue>> GetIncrementalChangesAsync(TInput input)
    {
        var aggregated = new ConcurrentDictionary<TRootEntity, TValue>();

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