
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class EmptyRootEntitiesProvider<TInput, TRootEntity, TSourceEntity> : IRootEntitiesProvider<TInput, TRootEntity, TSourceEntity>
{
    public async Task<IReadOnlyCollection<TRootEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TSourceEntity> entities)
    {
        return [];
    }
}