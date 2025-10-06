using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class ScopedEntityContextTransformer(
    Expression Expression
) : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context)
    {
        return new ScopedEntityContext(Expression, context);
    }
}
