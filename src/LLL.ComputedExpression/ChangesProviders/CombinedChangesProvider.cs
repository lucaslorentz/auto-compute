namespace LLL.ComputedExpression.ChangesProviders;

public class CombinedChangesProvider<TInput, TEntity, TValueA, TValueB, TValueC>(
    IChangesProvider<TInput, TEntity, TValueA> changesProviderA,
    IChangesProvider<TInput, TEntity, TValueB> changesProviderB,
    Func<TValueA, TValueB, TValueC> combineValue
) : IChangesProvider<TInput, TEntity, TValueC>
    where TEntity : class
{
    public async Task<IReadOnlyDictionary<TEntity, IValueChange<TValueC>>> GetChangesAsync(TInput input)
    {
        var changesFromA = await changesProviderA.GetChangesAsync(input);
        var changesFromB = await changesProviderB.GetChangesAsync(input);

        var result = new Dictionary<TEntity, IValueChange<TValueC>>();
        foreach (var entity in changesFromA.Keys.Union(changesFromB.Keys))
        {
            if (!changesFromA.TryGetValue(entity, out var valueA))
                valueA = await changesProviderA.GetChangeAsync(input, entity);

            if (!changesFromB.TryGetValue(entity, out var valueB))
                valueB = await changesProviderB.GetChangeAsync(input, entity);

            result[entity] = new LazyValueChange<TValueC>(
                () => combineValue(valueA.Original, valueB.Original),
                () => combineValue(valueA.Current, valueB.Current));
        }
        return result;
    }

    public async Task<IValueChange<TValueC>> GetChangeAsync(TInput input, TEntity entity)
    {
        var valueA = await changesProviderA.GetChangeAsync(input, entity);
        var valueB = await changesProviderB.GetChangeAsync(input, entity);

        return new LazyValueChange<TValueC>(
            () => combineValue(valueA.Original, valueB.Original),
            () => combineValue(valueA.Current, valueB.Current));
    }
}