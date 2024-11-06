using System.Linq.Expressions;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IUnboundChangesProvider
{
    LambdaExpression Expression { get; }
    EntityContext EntityContext { get; }
    IChangeCalculation ChangeCalculation { get; }
}

public interface IUnboundChangesProvider<TInput, TEntity, TChange>
    : IUnboundChangesProvider
    where TEntity : class
{
    IChangeCalculation IUnboundChangesProvider.ChangeCalculation => ChangeCalculation;
    new IChangeCalculation<TChange> ChangeCalculation { get; }
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TChange>? memory);
}