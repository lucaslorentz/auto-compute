using System.Linq.Expressions;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed);
    IChangesProvider? CreateChangesProvider(LambdaExpression computed);
    IIncrementalChangesProvider CreateIncrementalChangesProvider(IIncrementalComputed incrementalComputed);
    LambdaExpression GetOriginalValueExpression(LambdaExpression computed);
}
