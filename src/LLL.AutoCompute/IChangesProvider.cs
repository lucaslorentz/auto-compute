namespace LLL.AutoCompute;

public interface IChangesProvider<TEntity, TChange>
{
    string? ToDebugString();

    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync();
}