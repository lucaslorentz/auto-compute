using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IChangesProvider<TEntity, TChange>
{
    EntityContext EntityContext { get; }
    IChangeCalculation<TChange> ChangeCalculation { get; }
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync();
}