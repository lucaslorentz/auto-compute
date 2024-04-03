
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class EmptyRootEntitiesProvider : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return [];
    }
}