
namespace LLL.AutoCompute;

public interface IObservedProperty : IObservedMember<IObservedProperty>
{
    Type EntityType { get; }
}

public interface IObservedProperty<in TInput> : IObservedProperty
{
    Type IObservedMember.InputType => typeof(TInput);

    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IObservedMember.GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await GetAffectedEntitiesAsync(inputTyped, incrementalContext);
    }
}