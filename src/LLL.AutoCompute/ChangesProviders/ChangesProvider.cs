namespace LLL.AutoCompute.ChangesProviders;

public class ChangesProvider<TInput, TEntity, TChange>(
    IUnboundChangesProvider<TInput, TEntity, TChange> unboundChangesProvider,
    TInput input,
    ChangeMemory<TEntity, TChange> memory
) : IChangesProvider<TEntity, TChange>
    where TEntity : class
{
    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync()
    {
        return await unboundChangesProvider.GetChangesAsync(input, memory);
    }
}