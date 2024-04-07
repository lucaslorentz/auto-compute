namespace LLL.ComputedExpression;

public interface IRootEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities);
}

public interface IRootEntitiesProvider<in TInput, TRootEntity, TSourceEntity> : IRootEntitiesProvider
{
    Task<IReadOnlyCollection<TRootEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TSourceEntity> entities);

    async Task<IReadOnlyCollection<object>> IRootEntitiesProvider.GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        var entitiesTyped = entities.Cast<TSourceEntity>().ToArray();

        return (IReadOnlyCollection<object>)await GetRootEntitiesAsync(inputTyped, entitiesTyped);
    }
}