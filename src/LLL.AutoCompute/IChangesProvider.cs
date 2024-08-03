namespace LLL.AutoCompute;

public interface IChangesProvider<TEntity, TChange>
{
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync();
}