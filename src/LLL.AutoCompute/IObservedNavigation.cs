namespace LLL.AutoCompute;

public interface IObservedNavigation : IObservedMember
{
    IObservedEntityType SourceEntityType { get; }
    IObservedEntityType TargetEntityType { get; }
    bool IsCollection { get; }
    IObservedNavigation GetInverse();
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities);
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities);
    Task<ObservedNavigationChanges> GetChangesAsync(object input);
}

public interface IObservedNavigation<in TInput> : IObservedNavigation, IObservedMember<TInput>
    where TInput : ComputedInput
{
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(TInput input, IReadOnlyCollection<object> fromEntities);

    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(TInput input, IReadOnlyCollection<object> fromEntities);

    async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> IObservedNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await LoadCurrentAsync(inputTyped, fromEntities);
    }

    async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> IObservedNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await LoadOriginalAsync(inputTyped, fromEntities);
    }

    Task<ObservedNavigationChanges> GetChangesAsync(TInput input);

    async Task<ObservedNavigationChanges> IObservedNavigation.GetChangesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await GetChangesAsync(inputTyped);
    }
}
