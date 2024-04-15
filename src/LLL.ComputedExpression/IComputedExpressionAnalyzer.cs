using System.Linq.Expressions;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed);
    IChangesProvider? CreateChangesProvider(LambdaExpression computed, object? valueEqualityComparer = null);
    IIncrementalChangesProvider CreateIncrementalChangesProvider(IIncrementalComputed incrementalComputed);
    LambdaExpression GetOriginalValueExpression(LambdaExpression computed);
    LambdaExpression GetCurrentValueExpression(LambdaExpression computed);
}
