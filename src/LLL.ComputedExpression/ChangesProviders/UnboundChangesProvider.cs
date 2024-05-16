using System.Collections.Immutable;

namespace LLL.ComputedExpression.ChangesProviders;

public class UnboundChangesProvider<TInput, TEntity, TValue, TResult>(
    IAffectedEntitiesProvider<TInput, TEntity>? affectedEntitiesProvider,
    IChangeCalculation<TValue, TResult> changeCalculation,
    Func<TInput, IncrementalContext, TEntity, TValue> originalValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> currentValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> originalIncrementalValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> currentIncrementalValueGetter
) : IUnboundChangesProvider<TInput, TEntity, TResult>
    where TEntity : class
{
    public IChangeCalculation<TResult> ChangeCalculation => changeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TResult>> GetChangesAsync(
        TInput input,
        ChangeMemory<TEntity, TResult> changeMemory)
    {
        if (affectedEntitiesProvider is null)
            return ImmutableDictionary<TEntity, TResult>.Empty;

        var incrementalContext = new IncrementalContext();

        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input, incrementalContext);

        var changes = new Dictionary<TEntity, TResult>();

        foreach (var entity in affectedEntities)
        {
            changes[entity] = await GetChangeAsync(input, entity, incrementalContext, changeMemory);
        }

        foreach (var entity in changeMemory.GetEntities())
        {
            if (changes.ContainsKey(entity))
                continue;

            changes[entity] = await GetChangeAsync(input, entity, incrementalContext, changeMemory);
            changeMemory.Remove(entity);
        }

        var filteredChanges = changes
            .Where(kv => !changeCalculation.IsNoChange(kv.Value));

        return new Dictionary<TEntity, TResult>(filteredChanges);
    }

    public string? ToDebugString()
    {
        return affectedEntitiesProvider?.ToDebugString();
    }

    private async Task<TResult> GetChangeAsync(
        TInput input,
        TEntity entity,
        IncrementalContext incrementalContext,
        ChangeMemory<TEntity, TResult> changeMemory)
    {
        var valueChange = await changeCalculation.GetChangeAsync(CreateComputedValues(input, entity, incrementalContext));
        return DeltaChange(changeMemory, entity, valueChange);
    }

    private TResult DeltaChange(ChangeMemory<TEntity, TResult> changeMemory, TEntity entity, TResult result)
    {
        var delta = changeMemory.TryGet(entity, out var previousResult)
            ? ChangeCalculation.CalculateDelta(previousResult, result)
            : result;

        changeMemory.AddOrUpdate(entity, result);

        return delta;
    }

    private ComputedValues<TValue> CreateComputedValues(TInput input, TEntity entity, IncrementalContext incrementalContext)
    {
        return new ComputedValues<TValue>(
            () => originalValueGetter(input, incrementalContext, entity),
            () => currentValueGetter(input, incrementalContext, entity),
            () => originalIncrementalValueGetter(input, incrementalContext, entity),
            () => currentIncrementalValueGetter(input, incrementalContext, entity));
    }

    public record class ValueWrapper(TResult Value);
}