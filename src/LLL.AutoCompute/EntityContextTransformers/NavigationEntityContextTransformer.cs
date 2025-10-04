using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class NavigationEntityContextTransformer(IObservedNavigation navigation)
    : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context, Expression newNode)
    {
        return new NavigationEntityContext(newNode, context, navigation);
    }
}
