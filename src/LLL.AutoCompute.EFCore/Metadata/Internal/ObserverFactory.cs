using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public delegate Observer ObserverFactory<in TTarget>(
    IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer,
    TTarget target);

public class ObserverFactory
{
    public static ObserverFactory<IEntityType> CreateObserverFactory<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        IChangeCalculation<TValue, TChange> changeCalculation,
        Func<ComputedChangeEventData<TEntity, TChange>, Task> callback)
        where TEntity : class
    {
        return (analyzer, entityType) =>
        {
            try
            {
                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new Observer<TEntity, TChange>(
                    changesProvider,
                    callback);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for reaction in '{entityType.Name}': {ex.Message}", ex);
            }
        };
    }
}