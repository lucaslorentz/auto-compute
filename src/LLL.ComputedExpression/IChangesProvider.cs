namespace LLL.ComputedExpression;

public interface IChangesProvider
{
    Type InputType { get; }
    Type EntityType { get; }
    Type ValueType { get; }
    Task<IDictionary<object, IValueChange>> GetChangesAsync(object input);
    object ValueEqualityComparer { get; }
}

public interface IChangesProvider<in TInput, TEntity, TValue> : IChangesProvider
{
    Type IChangesProvider.InputType => typeof(TInput);
    Type IChangesProvider.EntityType => typeof(TEntity);
    Type IChangesProvider.ValueType => typeof(TValue);

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

    new IEqualityComparer<TValue> ValueEqualityComparer { get; }
    object IChangesProvider.ValueEqualityComparer => ValueEqualityComparer;
}