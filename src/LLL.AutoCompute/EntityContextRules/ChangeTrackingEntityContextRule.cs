using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextPropagators;

public class ChangeTrackingEntityContextRule : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry)
    {
        if (node is MethodCallExpression methodCallExpression
            && methodCallExpression.Method.DeclaringType == typeof(ChangeTrackingExtensions))
        {
            if (methodCallExpression.Method.Name == nameof(ChangeTrackingExtensions.AsComputedUntracked))
            {
                entityContextRegistry.RegisterPropagation(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    context => context.IsTrackingChanges
                        ? context.DeriveWithCache(("tracking", false), k => new ChangeTrackingEntityContext(node, context, false))
                        : context);
            }
            else if (methodCallExpression.Method.Name == nameof(ChangeTrackingExtensions.AsComputedTracked))
            {
                entityContextRegistry.RegisterPropagation(
                    methodCallExpression.Arguments[0],
                    EntityContextKeys.None,
                    node,
                    EntityContextKeys.None,
                    context => !context.IsTrackingChanges
                        ? context.DeriveWithCache(("tracking", true), k => new ChangeTrackingEntityContext(node, context, true))
                        : context);
            }
        }
    }
}
