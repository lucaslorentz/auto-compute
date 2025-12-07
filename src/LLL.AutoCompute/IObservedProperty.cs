
namespace LLL.AutoCompute;

public interface IObservedProperty : IObservedMember
{
    Type EntityType { get; }
    Task<ObservedPropertyChanges> GetChangesAsync(object input);
}

public interface IObservedProperty<in TInput> : IObservedProperty
    where TInput : IComputedInput
{
    Task<ObservedPropertyChanges> GetChangesAsync(TInput input);

    async Task<ObservedPropertyChanges> IObservedProperty.GetChangesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await GetChangesAsync(inputTyped);
    }
}