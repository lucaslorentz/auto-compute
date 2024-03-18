using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed.EntityContextPropagators;

public class NavigationEntityContextPropagator<TInput>(
    IEntityNavigationProvider navigationProvider
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        var navigationMatch = navigationProvider.GetEntityNavigation(node);
        if (navigationMatch != null)
        {
            var toKey = navigationMatch.Value.IsCollection
                ? EntityContextKeys.Element
                : EntityContextKeys.None;

            analysis.PropagateEntityContext(
                navigationMatch.FromExpression,
                EntityContextKeys.None,
                node,
                toKey,
                entityContext => new NavigationEntityContext(entityContext, navigationMatch.Value));
        }
    }
}
