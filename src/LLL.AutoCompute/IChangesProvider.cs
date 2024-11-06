using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IChangesProvider
{
    LambdaExpression Expression { get; }
    EntityContext EntityContext { get; }
    IChangeCalculation ChangeCalculation { get; }
}

public interface IChangesProvider<TEntity, TChange>
    : IChangesProvider
{
    IChangeCalculation IChangesProvider.ChangeCalculation => ChangeCalculation;
    new IChangeCalculation<TChange> ChangeCalculation { get; }
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync();
}