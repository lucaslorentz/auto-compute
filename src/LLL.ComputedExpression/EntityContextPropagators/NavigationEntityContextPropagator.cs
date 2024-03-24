using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed.EntityContextPropagators;

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
