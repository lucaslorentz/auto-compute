
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
            var originalValue = _entitiesLastValueChange.TryGetValue(entity, out var lastValueChange);
            _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
            result[entity] = new LazyValueChange<TValue>(
                () => lastValueChange is not null ? lastValueChange.Current : valueChange.Original,
                () => valueChange.Current);
        }
        foreach (var (entity, lastValueChange) in _entitiesLastValueChange)
        {
            if (result.ContainsKey(entity))
                continue;

            var valueChange = await GetChangeAsync(input, entity);
            _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
            result[entity] = new LazyValueChange<TValue>(
                () => lastValueChange is not null ? lastValueChange.Current : valueChange.Original,
                () => valueChange.Current);
        }
        return result;
    }
    public override async Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        _entitiesLastValueChange.TryGetValue(entity, out var previousValueChange);
        var valueChange = await changesProvider.GetChangeAsync(input, entity);
        _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
        return new LazyValueChange<TValue>(
            () => previousValueChange is not null ? previousValueChange.Current : valueChange.Original,
            () => valueChange.Current);
    }
}