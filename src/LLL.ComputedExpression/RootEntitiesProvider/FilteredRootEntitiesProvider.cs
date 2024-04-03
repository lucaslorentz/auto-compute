namespace LLL.ComputedExpression.RootEntitiesProvider;

public class FilteredRootEntitiesProvider(
    IRootEntitiesProvider parent,
    Delegate filter
) : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var filteredEntities = entities.Where(e => (bool)filter.DynamicInvoke(input, e)!).ToArray();
        return await parent.GetRootEntities(input, filteredEntities);
    }
}
