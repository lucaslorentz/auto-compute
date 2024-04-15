namespace LLL.ComputedExpression.ChangesProviders;

public abstract class ChangesProvider<TInput, TEntity, TValue>(
    IEqualityComparer<TValue> valueEqualityComparer
) : IChangesProvider<TInput, TEntity, TValue>
    where TEntity : class
{
    public IEqualityComparer<TValue> ValueEqualityComparer => valueEqualityComparer;

    public async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync(TInput input)
    {
        var changes = await GetUnfilteredChangesAsync(input);

        var filteredChanges = changes
            .Where(kv => !valueEqualityComparer.Equals(kv.Value.Original, kv.Value.Current));

        return new Dictionary<TEntity, IValueChange<TValue>>(filteredChanges);
    }
    public abstract Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity);

    protected abstract Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetUnfilteredChangesAsync(TInput input);
}