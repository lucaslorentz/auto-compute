using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IComputedExpressionAnalyzer<TInput>
{
    IUnboundChangesProvider<TInput, TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>> filterExpression,
        IChangeCalculation<TValue, TChange> changeCalculation)
        where TEntity : class;
}
