using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.ChangesProviders;

public class ComputedChangesProvider<TInput, TEntity, TValue, TChange>(
    Expression<Func<TEntity, TValue>> expression,
    EntityContext entityContext,
    Func<TEntity, bool> filter,
    EntityContext filterEntityContext,
    IChangeCalculator<TValue, TChange> changeCalculation,
    ComputedValueAccessors<TInput, TEntity, TValue> computedValueAccessors
) : IComputedChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
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
        var incrementalContext = new IncrementalContext();

        var affectedEntities = (await entityContext.GetAffectedEntitiesAsync(input!, incrementalContext))
            .OfType<TEntity>()
            .ToArray();

        await filterEntityContext.PreLoadNavigationsAsync(input!, affectedEntities);

        affectedEntities = affectedEntities
            .Where(e => entityContext.EntityType.GetEntityState(input!, e) != ObservedEntityState.Removed
                && filter(e))
            .ToArray();

        if (changeCalculation.IsIncremental)
            await entityContext.EnrichIncrementalContextAsync(input!, affectedEntities, incrementalContext);
        else if (changeCalculation.PreLoadEntities)
            await entityContext.PreLoadNavigationsAsync(input!, affectedEntities);

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
}