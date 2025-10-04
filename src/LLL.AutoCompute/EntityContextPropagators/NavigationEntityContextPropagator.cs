using System.Linq.Expressions;
using LLL.AutoCompute.EntityContextTransformers;

namespace LLL.AutoCompute.EntityContextPropagators;

public class NavigationEntityContextPropagator(
    HashSet<IObservedNavigationAccessLocator> navigationAccessLocators
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        foreach (var navigationAccessLocator in navigationAccessLocators)
        {
            var navigationAccess = navigationAccessLocator.GetObservedNavigationAccess(node);
            if (navigationAccess != null)
            {
                var toKey = navigationAccess.Navigation.IsCollection
                    ? EntityContextKeys.Element
                    : EntityContextKeys.None;

                analysis.PropagateEntityContext(
                    navigationAccess.FromExpression,
                    EntityContextKeys.None,
                    node,
                    toKey,
                    new NavigationEntityContextTransformer(navigationAccess.Navigation));
            }
        }
    }
}
