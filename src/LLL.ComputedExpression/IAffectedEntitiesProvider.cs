namespace L3.Computed;

public interface IAffectedEntitiesProvider
{
    Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input);

    string ToDebugString();
}

public interface IAffectedEntitiesProvider<in TInput> : IAffectedEntitiesProvider
{
    Task<IEnumerable<object>> GetAffectedEntitiesAsync(TInput input);

    Task<IEnumerable<object>> IAffectedEntitiesProvider.GetAffectedEntitiesAsync(object input)
    {
        return GetAffectedEntitiesAsync((TInput)input);
    }
}
