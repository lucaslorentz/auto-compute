using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class DistinctEntityContextTransformer
    : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context, Expression newNode)
    {
        return new DistinctEntityContext(newNode, context);
    }
}
