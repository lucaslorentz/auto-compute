using System.Collections.Immutable;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.ChangesProviders;

public class UnboundChangesProvider<TInput, TEntity, TValue, TChange>(
    IAffectedEntitiesProvider<TInput, TEntity>? affectedEntitiesProvider,
    EntityContext computedEntityContext,
    Func<TEntity, bool> filter,
    EntityContext filterEntityContext,
    IEntityActionProvider entityActionProvider,
    IChangeCalculation<TValue, TChange> changeCalculation,
    ComputedValueAccessors<TInput, TEntity, TValue> computedValueAccessors
) : IUnboundChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
{
    public IChangeCalculation<TChange> ChangeCalculation => changeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(
        TInput input,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        if (affectedEntitiesProvider is null)
            return ImmutableDictionary<TEntity, TChange>.Empty;

        var incrementalContext = new IncrementalContext();

        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input, incrementalContext);

        await filterEntityContext.PreLoadNavigationsAsync(input!, affectedEntities, incrementalContext);

        affectedEntities = affectedEntities
            .Where(e => entityActionProvider.GetEntityAction(input!, e) != EntityAction.Delete
                && filter(e))
            .ToArray();

        if (changeCalculation.IsIncremental)
            await computedEntityContext.EnrichIncrementalContextAsync(input!, affectedEntities, incrementalContext);
        else if (changeCalculation.PreLoadEntities)
            await computedEntityContext.PreLoadNavigationsAsync(input!, affectedEntities, incrementalContext);

        var changes = new Dictionary<TEntity, TChange>();

        foreach (var entity in affectedEntities)
        {
            changes[entity] = await GetChangeAsync(input, entity, incrementalContext, changeMemory);
        }

        if (changeMemory is not null)
        {
            foreach (var entity in changeMemory.GetEntities())
            {
                if (changes.ContainsKey(entity))
                    continue;

                changes[entity] = await GetChangeAsync(input, entity, incrementalContext, changeMemory);
                changeMemory.Remove(entity);
            }
        }

        var filteredChanges = changes
            .Where(kv => !changeCalculation.IsNoChange(kv.Value));

        return new Dictionary<TEntity, TChange>(filteredChanges);
    }

    public string? ToDebugString()
    {
        return affectedEntitiesProvider?.ToDebugString();
    }

    private async Task<TChange> GetChangeAsync(
        TInput input,
        TEntity entity,
        IncrementalContext incrementalContext,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        var valueChange = changeCalculation.GetChange(CreateComputedValues(input, entity, incrementalContext));
        return DeltaChange(changeMemory, entity, valueChange);
    }

    private TChange DeltaChange(ChangeMemory<TEntity, TChange>? changeMemory, TEntity entity, TChange result)
    {
        if (changeMemory is null)
            return result;

        var delta = changeMemory.TryGet(entity, out var previousResult)
            ? ChangeCalculation.DeltaChange(previousResult, result)
            : result;

        changeMemory.AddOrUpdate(entity, result);

        return delta;
    }

    private ComputedValues<TInput, TEntity, TValue> CreateComputedValues(TInput input, TEntity entity, IncrementalContext incrementalContext)
    {
        return new ComputedValues<TInput, TEntity, TValue>(
            input,
            incrementalContext,
            entity,
            computedValueAccessors);
    }

    public record class ValueWrapper(TChange Value);
}