using LLL.AutoCompute.ChangesProviders;

namespace LLL.AutoCompute;

public interface IUnboundChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
{
    string? ToDebugString();

    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TChange> memory);
}