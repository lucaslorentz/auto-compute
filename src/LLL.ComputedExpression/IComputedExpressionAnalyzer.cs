using System.Linq.Expressions;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed);
    IChangesProvider? GetChangesProvider(LambdaExpression computed);
    IIncrementalChangesProvider CreateIncrementalChangesProvider(IIncrementalComputed incrementalComputed);
    LambdaExpression GetOriginalValueExpression(LambdaExpression computed);
}
