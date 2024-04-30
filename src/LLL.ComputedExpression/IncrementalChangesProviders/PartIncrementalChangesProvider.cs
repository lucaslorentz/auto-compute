
using System.Collections.Concurrent;
using LLL.ComputedExpression.ChangesProviders;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProviders;

public class PartIncrementalChangesProvider<TInput, TRootEntity, TValue, TPartEntity>(
    IIncrementalComputed<TRootEntity, TValue> incrementalComputed,
    IChangesProvider<TInput, TPartEntity, TValue> valueChangesProvider,
    IChangesProvider<TInput, TPartEntity, IReadOnlyCollection<TRootEntity>> rootsChangesProvider
) : IIncrementalChangesProvider<TInput, TRootEntity, TValue>
    where TRootEntity : notnull
    where TPartEntity : class
    where TInput : IDeltaChangesInput
{
    readonly IChangesProvider<TInput, TPartEntity, PartChange<TValue, TRootEntity>> _changesProvider = valueChangesProvider
        .Combine(rootsChangesProvider, (value, rootEntities) => new PartChange<TValue, TRootEntity>(value, rootEntities), EqualityComparer<PartChange<TValue, TRootEntity>>.Default)
        .CreateDeltaChangesProvider();

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