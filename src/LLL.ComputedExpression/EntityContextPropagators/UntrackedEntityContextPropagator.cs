using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class UntrackedEntityContextPropagator<TInput> : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (node is MethodCallExpression methodCallExpression
            && methodCallExpression.Method.DeclaringType == typeof(UntrackedExtensions)
            && methodCallExpression.Method.Name == nameof(UntrackedExtensions.AsComputedUntracked))
            analysis.AddEntityContextProvider(node, (key) => new UntrackedEntityContext(typeof(TInput), node.Type, analysis.RootEntityType));
    }
}
