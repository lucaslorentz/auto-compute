using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class DistinctEntityContextTransformer(Expression Expression)
    : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context)
    {
        return new DistinctEntityContext(Expression, context);
    }
}
