using System.Linq.Expressions;
using L3.Computed.EntityContexts;

namespace L3.Computed.EntityContextResolvers;

public class NavigationEntityContextResolver<TInput>(
    IEntityNavigationProvider navigationProvider
) : IEntityContextResolver
{
    public IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key)
    {
        var navigation = navigationProvider.GetEntityNavigation(node);
        if (navigation != null)
        {
            var supportedKey = navigation.IsCollection
                ? EntityContextKeys.Element
                : EntityContextKeys.None;

            if (key != supportedKey)
                return null;

            var entityContext = analysis.ResolveEntityContext(navigation.SourceExpression, EntityContextKeys.None);
            if (entityContext == null)
                return null;

            return new NavigationEntityContext(entityContext, navigation);
        }

        return null;
    }
}
