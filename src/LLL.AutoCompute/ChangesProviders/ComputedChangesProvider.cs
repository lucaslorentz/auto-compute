using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.ChangesProviders;

public class ComputedChangesProvider<TEntity, TValue, TChange>(
    Expression<Func<TEntity, TValue>> expression,
    EntityContext entityContext,
    Func<TEntity, bool> filter,
    EntityContext filterEntityContext,
    IChangeCalculator<TValue, TChange> changeCalculation,
    Func<ComputedInput, TEntity, TValue> originalValueGetter,
    Func<ComputedInput, TEntity, TValue> currentValueGetter
) : IComputedChangesProvider<TEntity, TChange>
    where TEntity : class
{
    LambdaExpression IComputedChangesProvider.Expression => expression;
    public Expression<Func<TEntity, TValue>> Expression => expression;
    public EntityContext EntityContext => entityContext;
    public IReadOnlySet<IObservedMember> ObservedMembers { get; } = entityContext.GetAllObservedMembers().ToHashSet();
    public IChangeCalculator<TChange> ChangeCalculation => changeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(
        ComputedInput input,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        input.IncrementalContext = changeCalculation.ValueStrategy == ComputedValueStrategy.Incremental
            ? new IncrementalContext()
            : null;

        var affectedEntities = (await entityContext.GetAffectedEntitiesAsync(input))
            .OfType<TEntity>()
            .ToArray();

        await filterEntityContext.PreLoadNavigationsAsync(input!, affectedEntities);

        affectedEntities = affectedEntities
            .Where(e => entityContext.EntityType.GetEntityState(input!, e) != ObservedEntityState.Removed
                && filter(e))
            .ToArray();

        switch (changeCalculation.ValueStrategy)
        {
            case ComputedValueStrategy.Incremental:
                await entityContext.EnrichIncrementalContextAsync(input!, affectedEntities);
                break;
            case ComputedValueStrategy.Full:
                await entityContext.PreLoadNavigationsAsync(input!, affectedEntities);
                break;
        }

        var changes = new Dictionary<TEntity, TChange>();

        foreach (var entity in affectedEntities)
        {
            changes[entity] = await GetChangeAsync(input, entity, changeMemory);
        }

        if (changeMemory is not null)
        {
            foreach (var entity in changeMemory.GetEntities())
            {
                if (changes.ContainsKey(entity))
                    continue;

                changes[entity] = await GetChangeAsync(input, entity, changeMemory);
                changeMemory.Remove(entity);
            }
        }

        var filteredChanges = changes
            .Where(kv => !changeCalculation.IsNoChange(kv.Value));

        return new Dictionary<TEntity, TChange>(filteredChanges);
    }

    private async Task<TChange> GetChangeAsync(
        ComputedInput input,
        TEntity entity,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        var valueChange = changeCalculation.GetChange(CreateComputedValues(input, entity));
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

    private ComputedValues<TEntity, TValue> CreateComputedValues(ComputedInput input, TEntity entity)
    {
        return new ComputedValues<TEntity, TValue>(
            input,
            entity,
            originalValueGetter,
            currentValueGetter);
    }
}
