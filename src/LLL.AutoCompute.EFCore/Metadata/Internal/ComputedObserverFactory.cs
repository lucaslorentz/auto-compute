using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public delegate ComputedObserver ComputedObserverFactory<in TTarget>(
    IComputedExpressionAnalyzer<EFCoreComputedInput> analyzer,
    TTarget target);

public class ComputedObserverFactory
{
    public static ComputedObserverFactory<IEntityType> CreateObserverFactory<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculator<TValue, TChange> changeCalculation,
        Func<ComputedChangeEventData<TEntity, TChange>, Task> callback)
        where TEntity : class
    {
        return (analyzer, entityType) =>
        {
            try
            {
                var changesProvider = analyzer.CreateChangesProvider(
                    entityType.GetOrCreateObservedEntityType(),
                    computedExpression,
                    filterExpression ?? (static x => true),
                    changeCalculation);

                if (changesProvider.ObservedMembers.Count == 0)
                    throw new Exception("Computed expression doesn't have observed members");

                return new ComputedObserver<TEntity, TChange>(
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