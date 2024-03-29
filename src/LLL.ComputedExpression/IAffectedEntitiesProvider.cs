namespace LLL.Computed;

public interface IAffectedEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input);

    string ToDebugString();
}

public interface IAffectedEntitiesProvider<in TInput> : IAffectedEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(TInput input);

    Task<IReadOnlyCollection<object>> IAffectedEntitiesProvider.GetAffectedEntitiesAsync(object input)
    {
        return GetAffectedEntitiesAsync((TInput)input);
    }
}
