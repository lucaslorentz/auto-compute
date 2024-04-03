
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class NoOpRootEntitiesProvider : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return entities;
    }
}