using System.Linq.Expressions;

namespace LLL.Computed;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider CreateAffectedEntitiesProvider(LambdaExpression computed);
    LambdaExpression GetOldValueExpression(LambdaExpression computed);
}

