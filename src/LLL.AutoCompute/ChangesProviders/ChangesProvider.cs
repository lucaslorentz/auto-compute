using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.ChangesProviders;

public class ChangesProvider<TInput, TEntity, TChange>(
    IUnboundChangesProvider<TInput, TEntity, TChange> unboundChangesProvider,
    TInput input,
    ChangeMemory<TEntity, TChange> memory
) : IChangesProvider<TEntity, TChange>
    where TEntity : class
{
    LambdaExpression IChangesProvider.Expression => unboundChangesProvider.Expression;
    public EntityContext EntityContext => unboundChangesProvider.EntityContext;
    public IChangeCalculation<TChange> ChangeCalculation => unboundChangesProvider.ChangeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync()
    {
        return await unboundChangesProvider.GetChangesAsync(input, memory);
    }
}