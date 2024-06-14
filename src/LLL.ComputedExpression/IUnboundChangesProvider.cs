using LLL.ComputedExpression.ChangesProviders;

namespace LLL.ComputedExpression;

public interface IUnboundChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
{
    string? ToDebugString();

    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TChange> memory);
}