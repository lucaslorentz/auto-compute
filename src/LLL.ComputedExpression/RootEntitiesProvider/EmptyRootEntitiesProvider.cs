
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class EmptyRootEntitiesProvider<TInput, TRootEntity, TSourceEntity> : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntities(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        return [];
    }
}