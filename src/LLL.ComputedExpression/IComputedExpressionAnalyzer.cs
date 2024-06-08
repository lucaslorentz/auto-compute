using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer<TInput>
{
    IUnboundChangesProvider<TInput, TEntity, TChange>? GetChangesProvider<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> changeCalculationSelector)
        where TEntity : class;
}
