
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class CompositeRootEntitiesProvider(
    IReadOnlyCollection<IRootEntitiesProvider> providers
) : IRootEntitiesProvider
{
    public async Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var rootEntities = new HashSet<object>();
        foreach (var provider in providers)
        {
            foreach (var rootEntity in await provider.GetRootEntities(input, entities))
                rootEntities.Add(rootEntity);
        }
        return rootEntities;
    }
}