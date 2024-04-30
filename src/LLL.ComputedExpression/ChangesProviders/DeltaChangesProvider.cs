
using System.Runtime.CompilerServices;

namespace LLL.ComputedExpression.ChangesProviders;

public class DeltaChangesProvider<TInput, TEntity, TValue>(
    IChangesProvider<TInput, TEntity, TValue> changesProvider,
    IEqualityComparer<TValue> valueEqualityComparer
) : ChangesProvider<TInput, TEntity, TValue>(valueEqualityComparer)
    where TEntity : class
{
    private readonly ConditionalWeakTable<TEntity, IValueChange<TValue>> _entitiesLastValueChange = [];

    protected override async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetUnfilteredChangesAsync(TInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input);
        var result = new Dictionary<TEntity, IValueChange<TValue>>();
        foreach (var (entity, valueChange) in changes)
        {
            result[entity] = DeltafyChange(entity, valueChange);
        }
        foreach (var (entity, _) in _entitiesLastValueChange)
        {
            if (result.ContainsKey(entity))
                continue;

            result[entity] = await GetChangeAsync(input, entity);
        }
        return result;
    }

    public override async Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        var valueChange = await changesProvider.GetChangeAsync(input, entity);
        return DeltafyChange(entity, valueChange);
    }

    private IValueChange<TValue> DeltafyChange(TEntity entity, IValueChange<TValue> valueChange)
    {
        _entitiesLastValueChange.TryGetValue(entity, out var previousValueChange);
        _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
        return new LazyValueChange<TValue>(
            () => previousValueChange is not null ? previousValueChange.Current : valueChange.Original,
            () => valueChange.Current);
    }
}