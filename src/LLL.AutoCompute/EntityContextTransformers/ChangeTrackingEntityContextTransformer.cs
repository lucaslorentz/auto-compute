using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.EntityContextTransformers;

public record class ChangeTrackingEntityContextTransformer(bool TrackChanges)
    : IEntityContextTransformer
{
    public EntityContext Transform(EntityContext context, Expression newNode)
    {
        return new ChangeTrackingEntityContext(newNode, context, TrackChanges);
    }
}
