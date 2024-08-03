namespace LLL.AutoCompute;

public interface IEntityNavigation : IEntityMember<IEntityNavigation>
{
    Type SourceEntityType { get; }
    Type TargetEntityType { get; }
    bool IsCollection { get; }
    IEntityNavigation GetInverse();
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
    Task<IReadOnlyCollection<object>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
}

public interface IEntityNavigation<in TInput, TSourceEntity, TTargetEntity> : IEntityNavigation
{
    Type IEntityMember.InputType => typeof(TInput);
    Type IEntityNavigation.SourceEntityType => typeof(TSourceEntity);
    Type IEntityNavigation.TargetEntityType => typeof(TTargetEntity);

    new IEntityNavigation<TInput, TTargetEntity, TSourceEntity> GetInverse();

    IEntityNavigation IEntityNavigation.GetInverse()
    {
        return GetInverse();
    }

    Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities, IncrementalContext incrementalContext);

    Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IEntityNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadCurrentAsync(inputTyped, fromEntities.OfType<TSourceEntity>().ToArray(), incrementalContext);
    }

    async Task<IReadOnlyCollection<object>> IEntityNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadOriginalAsync(inputTyped, fromEntities.OfType<TSourceEntity>().ToArray(), incrementalContext);
    }

    Task<IReadOnlyCollection<TSourceEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext);
}
