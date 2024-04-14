namespace LLL.ComputedExpression.ChangesProviders;

public class SkipEqualsChangesProvider<TInput, TEntity, TValue>(
    IChangesProvider<TInput, TEntity, TValue> changesProvider,
    IEqualityComparer<TValue> valueEqualityComparer
) : IChangesProvider<TInput, TEntity, TValue>
    where TEntity : class
{
    public async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync(TInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input);

        var filteredChanges = changes
            .Where(kv => !valueEqualityComparer.Equals(kv.Value.Original, kv.Value.Current));

        return new Dictionary<TEntity, IValueChange<TValue>>(filteredChanges);
    }

    public async Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity)
    {
        return await changesProvider.GetChangeAsync(input, entity);
    }
}