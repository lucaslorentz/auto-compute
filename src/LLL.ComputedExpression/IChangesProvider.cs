namespace LLL.ComputedExpression;

public interface IChangesProvider<TEntity, TResult>
{
    string? ToDebugString();
    
    Task<IReadOnlyDictionary<TEntity, TResult>> GetChangesAsync();
}