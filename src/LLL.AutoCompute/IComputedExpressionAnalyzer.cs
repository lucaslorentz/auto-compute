using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IComputedExpressionAnalyzer
{
}

public interface IComputedExpressionAnalyzer<TInput> : IComputedExpressionAnalyzer
{
    IUnboundChangesProvider<TInput, TEntity, TChange> GetChangesProvider<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculation<TValue, TChange> changeCalculation)
        where TEntity : class;
}
