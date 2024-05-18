using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer<TInput>
{
    IUnboundChangesProvider<TInput, TEntity, TResult>? GetChangesProvider<TEntity, TValue, TResult>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<ChangeCalculationSelector<TValue, TResult>> changeCalculationSelector)
        where TEntity : class;
}
