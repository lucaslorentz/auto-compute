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
        return GetAffectedEntitiesAsync((TInput)input);
    }
}
