namespace LLL.ComputedExpression;

public interface IAffectedEntitiesProvider
{
    Type InputType { get; }
    Type EntityType { get; }

    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input);

    string ToDebugString();

    IReadOnlyCollection<IAffectedEntitiesProvider> Flatten()
    {
        return [this];
    }
}

public interface IAffectedEntitiesProvider<in TInput, TEntity> : IAffectedEntitiesProvider
{
    Type IAffectedEntitiesProvider.InputType => typeof(TInput);
    Type IAffectedEntitiesProvider.EntityType => typeof(TEntity);

    Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input);

    new IReadOnlyCollection<IAffectedEntitiesProvider<TInput, TEntity>> Flatten()
    {
        return [this];
    }

    async Task<IReadOnlyCollection<object>> IAffectedEntitiesProvider.GetAffectedEntitiesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await GetAffectedEntitiesAsync(inputTyped);
    }

    IReadOnlyCollection<IAffectedEntitiesProvider> IAffectedEntitiesProvider.Flatten()
    {
        return Flatten();
    }
}
