
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class NoOpRootEntitiesProvider<TInput, TEntity>
    : IRootEntitiesProvider<TInput, TEntity, TEntity>
{
    public async Task<IReadOnlyCollection<TEntity>> GetRootEntities(TInput input, IReadOnlyCollection<TEntity> entities)
    {
        return entities;
    }
}