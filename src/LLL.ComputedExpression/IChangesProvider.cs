namespace LLL.ComputedExpression;

public interface IChangesProvider
{
    Task<IDictionary<object, IValueChange>> GetChangesAsync(object input);
}

public interface IChangesProvider<in TInput, TEntity, TValue> : IChangesProvider
{
    Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync(TInput input);
    Task<IValueChange<TValue>> GetChangeAsync(TInput input, TEntity entity);

    async Task<IDictionary<object, IValueChange>> IChangesProvider.GetChangesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        var changes = await GetChangesAsync(inputTyped);

        return changes.ToDictionary(
            x => (object)x.Key!,
            x => (IValueChange)new LazyValueChange<object?>(
                () => x.Value.Original,
                () => x.Value.Current));
    }
}