using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityChangeTracker
{
    IEnumerable<IExpressionMatch<IAffectedEntitiesProvider>> TrackChanges(Expression node);
}

public interface IEntityChangeTracker<in TInput> : IEntityChangeTracker
{
    new IEnumerable<IExpressionMatch<IAffectedEntitiesProvider<TInput>>> TrackChanges(Expression node);

    IEnumerable<IExpressionMatch<IAffectedEntitiesProvider>> IEntityChangeTracker.TrackChanges(Expression node)
    {
        return TrackChanges(node);
    }
}
