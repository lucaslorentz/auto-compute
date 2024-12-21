
namespace LLL.AutoCompute;

public interface IObservedProperty : IObservedMember
{
    Type EntityType { get; }
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input);
}

public interface IObservedProperty<in TInput> : IObservedProperty, IObservedMember<TInput>
{
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(TInput input);

    async Task<IReadOnlyCollection<object>> IObservedProperty.GetAffectedEntitiesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await GetAffectedEntitiesAsync(inputTyped);
    }
}