namespace LLL.ComputedExpression;

public interface IRootEntitiesProvider
{
    Task<IReadOnlyCollection<object>> GetRootEntities(object input, IReadOnlyCollection<object> entities);
}