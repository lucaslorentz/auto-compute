using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IUnboundChangesProvider<TInput, TEntity, TChange>
    where TEntity : class
{
    EntityContext EntityContext { get; }
    IChangeCalculation<TChange> ChangeCalculation { get; }
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TChange> memory);
}