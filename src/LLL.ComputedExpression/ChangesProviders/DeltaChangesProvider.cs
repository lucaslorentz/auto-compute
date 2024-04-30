
using System.Runtime.CompilerServices;

namespace LLL.ComputedExpression.ChangesProviders;

public class DeltaChangesScope
{
}

public interface IDeltaChangesInput
{
    DeltaChangesScope DeltaChangesScope { get; }
}

public class DeltaChangesProvider<TInput, TEntity, TValue>(
    IChangesProvider<TInput, TEntity, TValue> changesProvider,
    IEqualityComparer<TValue> valueEqualityComparer
) : ChangesProvider<TInput, TEntity, TValue>(valueEqualityComparer)
    where TEntity : class
    where TInput : IDeltaChangesInput
{
    private readonly ConditionalWeakTable<DeltaChangesScope, ConditionalWeakTable<TEntity, IValueChange<TValue>>> _entitiesLastValueChange = [];

    protected override async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetUnfilteredChangesAsync(TInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input);
        var result = new Dictionary<TEntity, IValueChange<TValue>>();
        foreach (var (entity, valueChange) in changes)
        {
            result[entity] = DeltafyChange(input, entity, valueChange);
        }
        var lastValueChanges = _entitiesLastValueChange.GetOrCreateValue(input.DeltaChangesScope);
        foreach (var (entity, _) in lastValueChanges)
        {
            if (result.ContainsKey(entity))
                continue;

            result[entity] = await GetChangeAsync(input, entity);
            lastValueChanges.Remove(entity);
        }
        return result;
    }

    public override async Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        var valueChange = await changesProvider.GetChangeAsync(input, entity);
        return DeltafyChange(input, entity, valueChange);
    }

    private IValueChange<TValue> DeltafyChange(TInput input, TEntity entity, IValueChange<TValue> valueChange)
    {
        var lastValueChanges = _entitiesLastValueChange.GetOrCreateValue(input.DeltaChangesScope);
        lastValueChanges.TryGetValue(entity, out var previousValueChange);
        lastValueChanges.AddOrUpdate(entity, valueChange);
        return new LazyValueChange<TValue>(
            () => previousValueChange is not null ? previousValueChange.Current : valueChange.Original,
            () => valueChange.Current);
    }
}