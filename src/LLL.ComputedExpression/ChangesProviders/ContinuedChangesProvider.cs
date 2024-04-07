
using System.Runtime.CompilerServices;

namespace LLL.ComputedExpression.ChangesProviders;

public class ContinuedChangesProvider<TInput, TEntity, TValue>(
    IChangesProvider<TInput, TEntity, TValue> changesProvider
) : IChangesProvider<TInput, TEntity, TValue>
    where TEntity : class
{
    private readonly ConditionalWeakTable<TEntity, IValueChange<TValue>> _entitiesLastValueChange = [];

    public async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync(TInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input);
        var result = new Dictionary<TEntity, IValueChange<TValue>>();
        foreach (var (entity, valueChange) in changes)
        {
            _entitiesLastValueChange.TryGetValue(entity, out var previousValueChange);
            _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
            result[entity] = new LazyValueChange<TValue>(
                () => previousValueChange is not null ? previousValueChange.Current : valueChange.Original,
                () => valueChange.Current);
        }
        return result;
    }
    public async Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        _entitiesLastValueChange.TryGetValue(entity, out var previousValueChange);
        var valueChange = await changesProvider.GetChangeAsync(input, entity);
        _entitiesLastValueChange.AddOrUpdate(entity, valueChange);
        return new LazyValueChange<TValue>(
            () => previousValueChange is not null ? previousValueChange.Current : valueChange.Original,
            () => valueChange.Current);
    }
}