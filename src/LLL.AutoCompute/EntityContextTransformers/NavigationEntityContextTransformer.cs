using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class NavigationEntityContextTransformer(
    Expression Expression,
    IObservedNavigation navigation
) : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context)
    {
        return new NavigationEntityContext(Expression, context, navigation);
    }
}
