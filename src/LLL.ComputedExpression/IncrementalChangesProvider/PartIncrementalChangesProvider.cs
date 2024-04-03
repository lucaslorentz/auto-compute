
using System.Collections.Concurrent;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.IncrementalChangesProvider;

public class PartIncrementalChangesProvider(
    IIncrementalComputed incrementalComputed,
    IAffectedEntitiesProvider affectedEntitiesProvider,
    IEntityActionProvider entityActionProvider,
    Delegate originalValueGetter,
    Delegate currentValueGetter,
    IRootEntitiesProvider originalRootEntitiesProvider,
    IRootEntitiesProvider currentRootEntitiesProvider
) : IIncrementalChangesProvider
{
    public async Task<IDictionary<object, object?>> GetIncrementalChangesAsync(object input)
    {
        var incrementalChanges = new ConcurrentDictionary<object, object?>();

        foreach (var affectedEntity in await affectedEntitiesProvider.GetAffectedEntitiesAsync(input))
        {
            var affectedEntityAction = entityActionProvider.GetEntityAction(input, affectedEntity);

            var oldPartValue = affectedEntityAction == EntityAction.Create ? incrementalComputed.Zero : originalValueGetter.DynamicInvoke(input, affectedEntity);
            var oldRoots = affectedEntityAction == EntityAction.Create ? [] : await originalRootEntitiesProvider.GetRootEntities(input, [affectedEntity]);

            var newPartValue = affectedEntityAction == EntityAction.Delete ? incrementalComputed.Zero : currentValueGetter.DynamicInvoke(affectedEntity);
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