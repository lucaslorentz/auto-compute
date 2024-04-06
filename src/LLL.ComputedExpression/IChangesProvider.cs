namespace LLL.ComputedExpression;

public interface IChangesProvider
{
    Task<IDictionary<object, (object? OriginalValue, object? NewValue)>> GetChangesAsync(object input);
}

public interface IChangesProvider<in TInput, TEntity, TValue> : IChangesProvider
{
    Task<IReadOnlyDictionary<TEntity, (TValue? OriginalValue, TValue? NewValue)>> GetChangesAsync(TInput input);

    async Task<IDictionary<object, (object? OriginalValue, object? NewValue)>> IChangesProvider.GetChangesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        var changes = await GetChangesAsync(inputTyped);
        
        return changes.ToDictionary(
            x => (object)x.Key!,
            x => ((object?)x.Value.OriginalValue, (object?)x.Value.NewValue));
    }
}