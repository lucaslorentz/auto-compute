using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class ChangeTrackingEntityContextTransformer(
    Expression Expression,
    bool TrackChanges
) : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context)
    {
        return new ChangeTrackingEntityContext(Expression, context, TrackChanges);
    }
}
