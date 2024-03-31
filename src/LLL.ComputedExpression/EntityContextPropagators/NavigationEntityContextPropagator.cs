using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression.EntityContextPropagators;

public class NavigationEntityContextPropagator(
    HashSet<IEntityMemberAccessLocator<IEntityNavigation>> navigationAccessLocators
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        foreach (var navigationAccessLocator in navigationAccessLocators)
        {
            var navigationAccess = navigationAccessLocator.GetEntityMemberAccess(node);
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
