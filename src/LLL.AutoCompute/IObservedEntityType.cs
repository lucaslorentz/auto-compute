namespace LLL.AutoCompute;

public interface IObservedEntityType
{
    string Name { get; }
    Type InputType { get; }
    ObservedEntityState GetEntityState(IComputedInput input, object entity);
    bool IsInstanceOfType(object obj);
}

public interface IObservedEntityType<in TInput> : IObservedEntityType
{
    Type IObservedEntityType.InputType => typeof(TInput);

    ObservedEntityState GetEntityState(TInput input, object entity);

    ObservedEntityState IObservedEntityType.GetEntityState(IComputedInput input, object entity)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return GetEntityState(inputTyped, entity);
    }
}