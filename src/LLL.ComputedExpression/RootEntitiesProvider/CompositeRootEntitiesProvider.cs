
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class CompositeRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>(
    IReadOnlyCollection<IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>> providers
) : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        var rootEntities = new HashSet<TRootEntity>();
        foreach (var provider in providers)
        {
            foreach (var rootEntity in await provider.GetRootEntitiesAsync(input, entities))
                rootEntities.Add(rootEntity);
        }
        return rootEntities;
    }
}