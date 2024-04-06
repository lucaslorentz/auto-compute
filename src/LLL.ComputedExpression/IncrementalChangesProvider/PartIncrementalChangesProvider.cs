
using System.Collections.Concurrent;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProvider;

public class PartIncrementalChangesProvider<TInput, TRootEntity, TValue, TPartEntity>(
    IIncrementalComputed<TRootEntity, TValue> incrementalComputed,
    IAffectedEntitiesProvider<TInput, TPartEntity> affectedEntitiesProvider,
    IEntityActionProvider<TInput> entityActionProvider,
    Func<TInput, TPartEntity, TValue> originalValueGetter,
    Func<TInput, TPartEntity, TValue> currentValueGetter,
    IRootEntitiesProvider<TInput, TRootEntity, TPartEntity> originalRootEntitiesProvider,
    IRootEntitiesProvider<TInput, TRootEntity, TPartEntity> currentRootEntitiesProvider
) : IIncrementalChangesProvider<TInput, TRootEntity, TValue>
    where TRootEntity : notnull
{
    public async Task<IReadOnlyDictionary<TRootEntity, TValue>> GetIncrementalChangesAsync(TInput input)
    {
        var incrementalChanges = new ConcurrentDictionary<TRootEntity, TValue>();

        foreach (var affectedEntity in await affectedEntitiesProvider.GetAffectedEntitiesAsync(input))
        {
            var affectedEntityAction = entityActionProvider.GetEntityAction(input, affectedEntity!);

            var oldPartValue = affectedEntityAction == EntityAction.Create ? incrementalComputed.Zero : originalValueGetter(input, affectedEntity);
            var oldRoots = affectedEntityAction == EntityAction.Create ? [] : await originalRootEntitiesProvider.GetRootEntities(input, [affectedEntity]);

            var newPartValue = affectedEntityAction == EntityAction.Delete ? incrementalComputed.Zero : currentValueGetter(input, affectedEntity);
            var newRoots = affectedEntityAction == EntityAction.Delete ? [] : await currentRootEntitiesProvider.GetRootEntities(input, [affectedEntity]);

            foreach (var rootEntity in oldRoots)
            {
                incrementalChanges.AddOrUpdate(
                    rootEntity,
                    (k) => incrementalComputed.Remove(
                        incrementalComputed.Zero,
                        oldPartValue!
                    ),
                    (k, v) => incrementalComputed.Remove(
                        v,
                        oldPartValue!
                    )
                );
            }

            foreach (var rootEntity in newRoots)
            {
                incrementalChanges.AddOrUpdate(
                    rootEntity,
                    (k) => newPartValue!,
                    (k, v) => incrementalComputed.Add(
                        v,
                        newPartValue!
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