namespace LLL.ComputedExpression;

public interface IIncrementalChangesProvider
{
    Task<IReadOnlyDictionary<object, object?>> GetIncrementalChangesAsync(object input);
}

public interface IIncrementalChangesProvider<in TInput, TEntity, TValue> : IIncrementalChangesProvider
{
    Task<IReadOnlyDictionary<TEntity, TValue>> GetIncrementalChangesAsync(TInput input);

    async Task<IReadOnlyDictionary<object, object?>> IIncrementalChangesProvider.GetIncrementalChangesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        var changes = await GetIncrementalChangesAsync(inputTyped);

        return changes.ToDictionary(
            x => (object)x.Key!,
            x => (object?)x.Value);
    }
}
