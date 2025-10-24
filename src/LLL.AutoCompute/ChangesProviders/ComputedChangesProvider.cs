using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.ChangesProviders;

public class ComputedChangesProvider<TInput, TEntity, TValue, TChange>(
    Expression<Func<TEntity, TValue>> expression,
    EntityContext entityContext,
    Func<TEntity, bool> filter,
    EntityContext filterEntityContext,
    IChangeCalculator<TValue, TChange> changeCalculation,
    Func<TInput, TEntity, TValue> originalValueGetter,
    Func<TInput, TEntity, TValue> currentValueGetter
) : IComputedChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
    where TInput : IComputedInput
{
    LambdaExpression IComputedChangesProvider.Expression => expression;
    public Expression<Func<TEntity, TValue>> Expression => expression;
    public EntityContext EntityContext => entityContext;
    public IReadOnlySet<IObservedMember> ObservedMembers { get; } = entityContext.GetAllObservedMembers().ToHashSet();
    public IChangeCalculator<TChange> ChangeCalculation => changeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(
        TInput input,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        input.IncrementalContext = changeCalculation.IsIncremental
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

        if (input.IncrementalContext is not null)
            await entityContext.EnrichIncrementalContextAsync(input!, affectedEntities);
        else if (changeCalculation.PreLoadEntities)
            await entityContext.PreLoadNavigationsAsync(input!, affectedEntities);

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
        TInput input,
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

    private ComputedValues<TInput, TEntity, TValue> CreateComputedValues(TInput input, TEntity entity)
    {
        return new ComputedValues<TInput, TEntity, TValue>(
            input,
            entity,
            originalValueGetter,
            currentValueGetter);
    }
}