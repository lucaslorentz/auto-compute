using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class NavigationEntityContextPropagator(
    HashSet<IEntityNavigationAccessLocator> navigationAccessLocators
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        foreach (var navigationAccessLocator in navigationAccessLocators)
        {
            var navigationAccess = navigationAccessLocator.GetEntityNavigationAccess(node);
            if (navigationAccess != null)
            {
                var toKey = navigationAccess.Member.IsCollection
                    ? EntityContextKeys.Element
                    : EntityContextKeys.None;

                analysis.PropagateEntityContext(
                    navigationAccess.FromExpression,
                    EntityContextKeys.None,
                    node,
                    toKey,
                    entityContext => new NavigationEntityContext(entityContext, navigationAccess.Member));
            }
        }
    }
}
