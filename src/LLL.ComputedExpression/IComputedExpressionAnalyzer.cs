using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed);
    LambdaExpression GetOriginalValueExpression(LambdaExpression computed);
    EntityContext GetEntityContext(
        LambdaExpression computed,
        Expression node,
        string entityContextKey);
}

