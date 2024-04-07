
namespace LLL.ComputedExpression.RootEntitiesProvider;

public class NoOpRootEntitiesProvider<TInput, TEntity>
    : IRootEntitiesProvider<TInput, TEntity, TEntity>
{
    public async Task<IReadOnlyCollection<TEntity>> GetRootEntitiesAsync(TInput input, IReadOnlyCollection<TEntity> entities)
    {
        return entities;
    }
}