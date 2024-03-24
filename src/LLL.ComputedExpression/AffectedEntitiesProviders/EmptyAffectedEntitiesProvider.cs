namespace LLL.Computed.AffectedEntitiesProviders;

public class EmptyAffectedEntitiesProvider
    : IAffectedEntitiesProvider
{
    public string ToDebugString()
    {
        return $"Empty";
    }

    public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(object input)
    {
        return [];
    }
}
