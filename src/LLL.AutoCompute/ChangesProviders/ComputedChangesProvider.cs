using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.ChangesProviders;

public class ComputedChangesProvider<TEntity, TValue, TChange>(
    Expression<Func<TEntity, TValue>> expression,
    EntityContext entityContext,
    Func<TEntity, bool> filter,
    EntityContext filterEntityContext,
    IChangeCalculator<TValue, TChange> changeCalculator,
    Func<ComputedInput, TEntity, TValue> originalValueGetter,
    Func<ComputedInput, TEntity, TValue> currentValueGetter
) : IComputedChangesProvider<TEntity, TChange>
    where TEntity : class
{
    LambdaExpression IComputedChangesProvider.Expression => expression;
    public Expression<Func<TEntity, TValue>> Expression => expression;
    public EntityContext EntityContext => entityContext;
    public IChangeCalculator<TChange> ChangeCalculator => changeCalculator;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(
        ComputedInput input,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        try
        {
            if (changeCalculator.ValueStrategy == ComputedValueStrategy.Incremental)
                input.Set(new IncrementalContext());
            else
                input.Remove<IncrementalContext>();

            var affectedEntities = (await entityContext.GetAffectedEntitiesAsync(input))
                .OfType<TEntity>()
                .ToArray();

            await filterEntityContext.PreLoadNavigationsAsync(input!, affectedEntities);

            affectedEntities = affectedEntities
                .Where(e => entityContext.EntityType.GetEntityState(input!, e) != ObservedEntityState.Removed
                    && filter(e))
                .ToArray();

            switch (changeCalculator.ValueStrategy)
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
                .Where(kv => !changeCalculator.IsNoChange(kv.Value));

            return new Dictionary<TEntity, TChange>(filteredChanges);
        }
        finally
        {
            input.Remove<IncrementalContext>();
        }
    }

    private async Task<TChange> GetChangeAsync(
        ComputedInput input,
        TEntity entity,
        ChangeMemory<TEntity, TChange>? changeMemory)
    {
        var valueChange = changeCalculator.GetChange(CreateComputedValues(input, entity));
        return DeltaChange(entity, valueChange, changeMemory);
    }

    private TChange DeltaChange(TEntity entity, TChange change, ChangeMemory<TEntity, TChange>? changeMemory)
    {
        if (changeMemory is null)
            return change;

        var delta = changeMemory.TryGet(entity, out var previousChange)
            ? ChangeCalculator.DeltaChange(previousChange, change)
            : change;

        changeMemory.AddOrUpdate(entity, change);

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
