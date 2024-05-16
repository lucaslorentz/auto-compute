using LLL.ComputedExpression.ChangesProviders;

namespace LLL.ComputedExpression;

public interface IUnboundChangesProvider<TInput, TEntity, TResult>
    where TEntity : class
{
    string? ToDebugString();

    Task<IReadOnlyDictionary<TEntity, TResult>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TResult> memory);
}