using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed);
    LambdaExpression GetOriginalValueExpression(LambdaExpression computed);
    EntityContext GetEntityContext(
        LambdaExpression computed,
        Expression node,
        string entityContextKey);
}

