
using System.Collections.Concurrent;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProvider;

public class PartIncrementalChangesProvider<TInput, TRootEntity, TValue, TPartEntity>(
    IIncrementalComputed<TRootEntity, TValue> incrementalComputed,
    IChangesProvider<TInput, TPartEntity, TValue> valueChangesProvider,
    IChangesProvider<TInput, TPartEntity, IReadOnlyCollection<TRootEntity>> rootsChangesProvider
) : IIncrementalChangesProvider<TInput, TRootEntity, TValue>
    where TRootEntity : notnull
    where TPartEntity : class
{
    readonly IChangesProvider<TInput, TPartEntity, (TValue Value, IReadOnlyCollection<TRootEntity> Roots)> _changesProvider = valueChangesProvider
        .SkipEqualValues(incrementalComputed.GetValueEqualityComparer())
        .Combine(rootsChangesProvider, (a, b) => (Value: a, Roots: b))
        .CreateContinuedChangesProvider();

    public async Task<IReadOnlyDictionary<TRootEntity, TValue>> GetIncrementalChangesAsync(TInput input)
    {
        var incrementalChanges = new ConcurrentDictionary<TRootEntity, TValue>();

        foreach (var kv in await _changesProvider.GetChangesAsync(input))
        {
            foreach (var rootEntity in kv.Value.Original.Roots)
            {
                incrementalChanges.AddOrUpdate(
                    rootEntity,
                    (k) => incrementalComputed.Remove(
                        incrementalComputed.Zero,
                        kv.Value.Original.Value
                    ),
                    (k, v) => incrementalComputed.Remove(
                        v,
                        kv.Value.Original.Value
                    )
                );
            }

            foreach (var rootEntity in kv.Value.Current.Roots)
            {
                incrementalChanges.AddOrUpdate(
                    rootEntity,
                    (k) => kv.Value.Current.Value,
                    (k, v) => incrementalComputed.Add(
                        v,
                        kv.Value.Current.Value
                    )
                );
            }
        }

        foreach (var kv in incrementalChanges)
        {
            if (incrementalComputed.IsZero(kv.Value))
                incrementalChanges.Remove(kv.Key, out var _);
        }

        return incrementalChanges;
    }
}