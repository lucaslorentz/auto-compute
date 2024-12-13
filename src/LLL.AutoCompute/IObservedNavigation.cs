namespace LLL.AutoCompute;

public interface IObservedNavigation : IObservedMember
{
    Type SourceEntityType { get; }
    Type TargetEntityType { get; }
    bool IsCollection { get; }
    IObservedNavigation GetInverse();
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
    Task<IReadOnlyCollection<object>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);
}

public interface IObservedNavigation<in TInput> : IObservedNavigation, IObservedMember<TInput>
{
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(TInput input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);

    Task<IReadOnlyCollection<object>> LoadOriginalAsync(TInput input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IObservedNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await LoadCurrentAsync(inputTyped, fromEntities, incrementalContext);
    }

    async Task<IReadOnlyCollection<object>> IObservedNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await LoadOriginalAsync(inputTyped, fromEntities, incrementalContext);
    }
}
