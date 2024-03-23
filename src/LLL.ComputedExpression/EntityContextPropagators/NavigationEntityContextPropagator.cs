using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed.EntityContextPropagators;

public class NavigationEntityContextPropagator<TInput>(
    IEntityMemberAccessLocator<IEntityNavigation> navigationProvider
) : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        var navigationAccess = navigationProvider.GetEntityMemberAccess(node);
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
