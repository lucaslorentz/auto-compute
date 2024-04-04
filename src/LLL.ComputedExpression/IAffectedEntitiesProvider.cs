namespace LLL.ComputedExpression;

public interface IAffectedEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input);

    string ToDebugString();

    IReadOnlyCollection<IAffectedEntitiesProvider> Flatten() {
        return [this];
    }
}

public interface IAffectedEntitiesProvider<in TInput> : IAffectedEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(TInput input);

    Task<IReadOnlyCollection<object>> IAffectedEntitiesProvider.GetAffectedEntitiesAsync(object input)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return GetAffectedEntitiesAsync(inputTyped);
    }
}
