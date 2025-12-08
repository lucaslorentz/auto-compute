using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IComputedExpressionAnalyzer
{
    IComputedChangesProvider<TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>> filterExpression,
        IChangeCalculator<TValue, TChange> changeCalculation)
        where TEntity : class;

    Expression RunExpressionModifiers(Expression expression);
}
