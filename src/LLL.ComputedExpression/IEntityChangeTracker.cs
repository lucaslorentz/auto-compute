using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityChangeTracker
{
    IEnumerable<IEntityMemberAccess<IAffectedEntitiesProvider>> TrackChanges(Expression node);
}

public interface IEntityChangeTracker<in TInput> : IEntityChangeTracker
{
    new IEnumerable<IEntityMemberAccess<IAffectedEntitiesProvider<TInput>>> TrackChanges(Expression node);

    IEnumerable<IEntityMemberAccess<IAffectedEntitiesProvider>> IEntityChangeTracker.TrackChanges(Expression node)
    {
        return TrackChanges(node);
    }
}
