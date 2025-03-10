using System.Linq.Expressions;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IComputedChangesProvider
{
    LambdaExpression Expression { get; }
    EntityContext EntityContext { get; }
    IReadOnlySet<IObservedMember> ObservedMembers { get; }
    IChangeCalculation ChangeCalculation { get; }
}

public interface IComputedChangesProvider<TInput, TEntity, TChange>
    : IComputedChangesProvider
    where TEntity : class
{
    IChangeCalculation IComputedChangesProvider.ChangeCalculation => ChangeCalculation;
    new IChangeCalculation<TChange> ChangeCalculation { get; }
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(TInput input, ChangeMemory<TEntity, TChange>? memory);
}