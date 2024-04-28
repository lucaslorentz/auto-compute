namespace LLL.ComputedExpression.ChangesProviders;

public class ChangesProvider<TInput, TEntity, TResult>(
    IUnboundChangesProvider<TInput, TEntity, TResult> unboundChangesProvider,
    TInput input,
    ChangeMemory<TEntity, TResult> memory
) : IChangesProvider<TEntity, TResult>
    where TEntity : class
{
    public async Task<IReadOnlyDictionary<TEntity, TResult>> GetChangesAsync()
    {
        return await unboundChangesProvider.GetChangesAsync(input, memory);
    }

    public string? ToDebugString()
    {
        return unboundChangesProvider?.ToDebugString();
    }
}