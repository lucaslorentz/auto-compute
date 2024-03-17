using System.Linq.Expressions;

namespace L3.Computed;

public interface IComputedExpressionAnalyzer
{
    IAffectedEntitiesProvider CreateAffectedEntitiesProvider(LambdaExpression computed);
}

