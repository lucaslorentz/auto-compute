namespace LLL.AutoCompute;

public interface IObservedNavigation : IObservedMember<IObservedNavigation>
{
    Type SourceEntityType { get; }
    Type TargetEntityType { get; }
    bool IsCollection { get; }
    IObservedNavigation GetInverse();
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
    Task<IReadOnlyCollection<object>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
}

public interface IObservedNavigation<in TInput, TSourceEntity, TTargetEntity> : IObservedNavigation
{
    Type IObservedMember.InputType => typeof(TInput);
    Type IObservedNavigation.SourceEntityType => typeof(TSourceEntity);
    Type IObservedNavigation.TargetEntityType => typeof(TTargetEntity);

    new IObservedNavigation<TInput, TTargetEntity, TSourceEntity> GetInverse();

    IObservedNavigation IObservedNavigation.GetInverse()
    {
        return GetInverse();
    }

    Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities, IncrementalContext incrementalContext);

    Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IObservedNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadCurrentAsync(inputTyped, fromEntities.Cast<TSourceEntity>().ToArray(), incrementalContext);
    }

    async Task<IReadOnlyCollection<object>> IObservedNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadOriginalAsync(inputTyped, fromEntities.Cast<TSourceEntity>().ToArray(), incrementalContext);
    }

    Task<IReadOnlyCollection<TSourceEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IObservedMember.GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await GetAffectedEntitiesAsync(inputTyped, incrementalContext);
    }
}
