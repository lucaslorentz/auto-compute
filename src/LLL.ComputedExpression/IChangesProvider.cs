namespace LLL.ComputedExpression;

public interface IChangesProvider<TEntity, TChange>
{
    string? ToDebugString();

    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync();
}